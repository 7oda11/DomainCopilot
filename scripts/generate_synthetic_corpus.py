"""Generate deterministic, fictional Domain Copilot assessment PDFs."""

from __future__ import annotations

import hashlib
import json
import os
from functools import partial
from pathlib import Path

# ReportLab uses SOURCE_DATE_EPOCH for stable PDF creation and modification dates.
os.environ["SOURCE_DATE_EPOCH"] = "946684800"  # 2000-01-01T00:00:00Z

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import inch
from reportlab.pdfgen.canvas import Canvas
from reportlab.platypus import PageBreak, Paragraph, SimpleDocTemplate, Spacer


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "data" / "synthetic" / "corpus"
MANIFEST = ROOT / "data" / "synthetic" / "corpus-manifest.json"
PAGE_COUNT = 5


# All records are invented assessment fixtures, not public clinical guidance.
# Keep source names stable because the evaluation set uses them as relevance labels.
DOCUMENTS = [
    ("synthetic-adult-screening-v1.pdf", "Adult Screening Record Workflow", "adult screening visit documentation", "The fictional form has a blood pressure screening field and a separate completion marker. It describes where staff record a measurement supplied by an approved local process; it defines no thresholds, frequency, diagnosis, or treatment action.", "Blood pressure and screening record completeness are reviewed as separate fields. A blank field is recorded as unavailable rather than inferred. This workflow tests source and citation retrieval only.", "Record the source form, section, version, page and review date. Keep the recorded value attached to the original workflow context. Never derive a clinical interpretation from a blank field or from this synthetic document."),
    ("synthetic-warning-signs-v1.pdf", "Warning Sign Record Completeness", "warning signs in an urgent assessment record", "The synthetic form labels missing timestamps, missing source attribution and unresolved identity fields as warning signs for urgent assessment record review. These are administrative completeness prompts, not clinical triage rules.", "The reviewer marks which record fields are missing, identifies the source page and routes unresolved administrative issues using the local review pathway. No clinical urgency threshold is defined here.", "Document the warning signs exactly as recorded, distinguish administrative flags from recorded findings and avoid turning a blank field into an unsupported fact."),
    ("synthetic-follow-up-v1.pdf", "Record Review Follow-up Version One", "documentation follow-up interval and version conflict handling", "This fictional record-review policy version 1 states that documentation follow-up for the synthetic audit queue is scheduled every 7 calendar days. The interval applies only to reviewing missing record fields; it is not an appointment schedule.", "The queue owner checks the record review date, version label and outstanding documentation fields. When another version differs, retain both source references and ask the policy owner to resolve the conflict; do not silently merge intervals.", "Cite the exact version and page. This intentionally conflicts with version 2 to exercise retrieval across sources. Both values are invented administrative examples with no clinical meaning."),
    ("synthetic-medication-history-v1.pdf", "Medication History Field Guide", "medication history field documentation", "The synthetic history form provides fields for medication name, recorded source, date obtained and a status indicating whether the information is complete, partial or unavailable. It supplies no medication advice.", "Staff record the medication name as provided by an authorized source and preserve uncertainty in the status field. A missing entry stays unknown until verified through an approved workflow.", "Record who supplied the information, when it was obtained and which form version was used. This document does not authorize a clinical conclusion."),
    ("synthetic-infection-control-v1.pdf", "Entry Checklist for Shared Clinical Rooms", "hand hygiene before patient contact", "The fictional room-entry checklist includes a hand hygiene checkpoint before patient contact, a clean-work-surface status and a completion timestamp. It is a training fixture and does not replace local approved procedures.", "The checklist records whether each listed administrative checkpoint was completed, deferred or unavailable. Staff use the current locally approved procedure for any real-world action.", "Record checklist version, location code and completion status without entering personal details into the assessment system."),
    ("synthetic-allergy-documentation-v1.pdf", "Allergy Record Uncertainty Guide", "documenting an unknown reaction", "The synthetic form distinguishes a reported allergy from an unknown reaction. When the reaction is not available in the source, document the reaction field as unknown instead of filling it with an assumption.", "Keep the source statement separate from reviewer comments. Route incomplete information to the designated verification process; this fixture does not evaluate allergy severity or treatment.", "Preserve source, date and version. An unknown reaction remains unknown until an authorized source resolves it."),
    ("synthetic-escalation-pathway-v1.pdf", "Administrative Escalation Handoff", "escalation for an acute change in a record", "This fictional record workflow routes an acute change in a submitted record to the designated reviewer queue. The phrase acute change refers to a change in the record state, not a clinical observation or a medical triage rule.", "Attach the original and updated record references, note the time observed and notify the role named by the local process. Do not invent a clinical interpretation or urgency category.", "Track escalation owner, acknowledgement and disposition. If the real-world issue needs clinical judgment, direct it to an appropriately qualified clinician outside this assessment fixture."),
    ("synthetic-handover-standard-v1.pdf", "Handover Record Structure", "clinical handover fields", "The sample clinical handover record includes the event time, a source reference, pending documentation and the responsible clinician field. It does not prescribe treatment or transfer clinical instructions.", "The author identifies what is known, what remains pending and which approved source supports each statement. The receiver acknowledges the handover using the local workflow.", "Keep the clinical handover title, time and responsible clinician attribution visible in the saved record. Missing details are marked unknown, not guessed."),
    ("synthetic-consent-policy-v1.pdf", "Consent Decision Record Template", "recording a patient decision in a consent workflow", "The fictional consent template records that a consent discussion occurred, the patient decision as documented by an authorized professional and the source/version of the approved form. It does not provide legal or clinical consent advice.", "The template separates the patient decision from administrative completion checks. Questions outside the record are directed to the responsible professional under the local process.", "Record the time, form version and author. Do not use this sample to infer consent capacity, substitute for a discussion or create a decision that was not documented."),
    ("synthetic-adverse-event-v1.pdf", "Event Reporting Queue Guide", "reporting a suspected adverse event", "This sample process explains how an adverse event entry is reported to a review queue. The fixture describes record routing and fields only; it does not define clinical causality or severity.", "Capture the event reference, reported time, source and status. Route the report to the named reviewer and keep the reporter's statement distinct from later analysis.", "A report is not a finding of causation. Preserve edits in the audit record and follow the approved local procedure for real events."),
    ("synthetic-respiratory-checklist-v1.pdf", "Respiratory Observation Record Fields", "respiratory assessment checklist observation fields", "The fictional respiratory checklist has separate fields labelled respiratory rate and oxygen saturation, plus source, timestamp and completion status. It contains no thresholds, interpretations or treatment recommendations.", "Each value is transcribed only from its identified source. Missing values remain blank or unknown under the record rules; this document does not calculate a conclusion.", "Cite the exact checklist version and page. A qualified professional must interpret real observations using current approved guidance."),
    ("synthetic-identity-check-v1.pdf", "Procedure Record Identity Fields", "identity checks before procedure record completion", "The synthetic procedure checklist asks the author to verify two identifiers before procedure record completion. The fixture tests that an ordered documentation checkpoint remains visible.", "The reviewer marks the two identifier fields complete or unresolved before proceeding with record completion. If a field is unavailable or inconsistent, pause the administrative workflow and route it to the designated owner.", "Never place real identifiers in this dataset. The sample uses only blank fictional fields and is not a patient-care protocol."),
    ("synthetic-records-policy-v1.pdf", "Unavailable Records Documentation Rule", "documenting unavailable clinical records", "The fictional records policy says unavailable records must be documented as unavailable, with the attempted source and time noted. A missing record must not be represented as reviewed.", "The user records the system/source checked and the result. When a source cannot be reached, the work item remains pending and follows the local escalation route.", "Keep provenance and version metadata with each note. Do not reconstruct missing clinical content from memory or model output."),
    ("synthetic-note-review-v1.pdf", "Draft Clinical Note Review Workflow", "clinician approval of a draft clinical note", "The sample workflow requires clinician approval before a draft clinical note is treated as reviewed. Draft status and approval status remain distinct, traceable fields.", "The drafter submits source-linked statements. The clinician may approve, reject or edit with a recorded comment; a rejection returns the draft to pending work.", "Record reviewer identity, decision, timestamp and version. This fixture does not authorize automatic publication or any consequential action before approval."),
    ("synthetic-specimen-handling-v1.pdf", "Specimen Record Tracking Fields", "specimen label and protective equipment checklist fields", "The synthetic specimen form contains a specimen label verification field and a protective equipment checklist field. It defines document capture only, not laboratory or handling instructions.", "The author records the form identifier, source and field status. Any real handling procedure comes from the organization's approved current instructions, not this fixture.", "No real specimen or personal data is included. Keep the record linked to its synthetic document version."),
    ("synthetic-discharge-checklist-v1.pdf", "Discharge Documentation Checklist", "discharge instructions and documentation follow-up fields", "The fictional discharge checklist has fields for discharge instructions supplied by an authorized source, a follow-up record reference and a completion status. It does not give patient-specific instructions or timelines.", "The author records only the approved source content and marks unavailable details as pending. A reviewer checks that citations resolve to the exact source chunk.", "Preserve the source version and author. Do not invent follow-up actions, schedules or treatment from this checklist."),
    ("synthetic-follow-up-v2.pdf", "Record Review Follow-up Version Two", "documentation follow-up interval and version conflict handling", "This fictional record-review policy version 2 states that documentation follow-up for the synthetic audit queue is scheduled every 14 calendar days. The interval applies only to reviewing missing record fields; it is not a patient appointment or treatment schedule.", "The queue owner checks the record review date, version label and outstanding documentation fields. When another version differs, retain both source references and ask the policy owner to resolve the conflict; do not silently merge intervals.", "Cite the exact version and page. This intentionally conflicts with version 1 to exercise retrieval across sources. Both values are invented administrative examples with no clinical meaning."),
    ("synthetic-referral-tracking-v1.pdf", "Referral Record Tracking Fields", "tracking a referral source and status", "The sample referral register identifies referral source, received time, destination role and current administrative status. It contains no eligibility or treatment determination.", "Each status change is recorded with an actor and timestamp. Unavailable destination details are escalated to the workflow owner rather than inferred.", "Keep source references attached to the referral entry and preserve its synthetic version label."),
    ("synthetic-privacy-minimum-v1.pdf", "Assessment Data Minimization Rules", "minimum necessary synthetic data handling", "The assessment dataset uses invented document identifiers and contains no real personal information. The sample privacy rule directs contributors to omit personal identifiers from test cases and logs.", "Review generated data before publication. Remove accidental personal information and retain only the fields needed to demonstrate the software workflow.", "Do not upload real records. Report suspected exposure through the repository's security process and remove the affected artifact before sharing."),
    ("synthetic-equipment-log-v1.pdf", "Equipment Record Maintenance Log", "equipment identifier and maintenance status fields", "The fictional log records an equipment code, inspection source, recorded date and maintenance status. It is not a maintenance manual and contains no operating instructions.", "A status transition records the source and responsible role. Unclear entries remain unresolved until the designated reviewer responds.", "Keep historical versions and timestamps so an assessment reviewer can reproduce the record trail."),
    ("synthetic-incident-review-v1.pdf", "Incident Review Queue Fields", "incident review status and evidence attribution", "The sample incident queue distinguishes the reporter's description, attached evidence references and reviewer findings. A report alone is not a verified conclusion.", "Reviewers record the source reviewed, outstanding questions and next administrative owner. Conflicting evidence is preserved for qualified human review.", "Maintain a decision audit trail. Do not convert model-generated text into a finding without human verification."),
    ("synthetic-accessible-communication-v1.pdf", "Communication Preference Record", "recording accessibility and communication preferences", "The synthetic record contains an optional communication preference field and a source attribution field. It avoids assumptions about an individual's needs.", "Record only the preference explicitly supplied through an approved process. If no preference is available, mark the field unavailable rather than selecting a default.", "Use respectful, neutral wording and retain the date and source of the entry."),
    ("synthetic-downtime-procedure-v1.pdf", "Application Downtime Record Procedure", "logging an unavailable application during downtime", "The sample downtime form captures system name, start time, affected workflow and a recovery status. It does not instruct users to bypass security controls.", "Record attempted actions and known status. When normal service resumes, reconcile the pending record through the authorized recovery workflow.", "Protect credentials and personal information. Keep the synthetic exercise isolated from production systems."),
    ("synthetic-audit-trail-v1.pdf", "Audit Trail Event Structure", "audit event actor timestamp and object reference", "The fictional audit schema stores event type, actor role, timestamp, object identifier and a short reason. It excludes secrets and unnecessary raw record content.", "Each state transition appends a new event instead of overwriting the previous decision. A reviewer can follow events in chronological order.", "Retain source version and correlation reference when available. An audit event does not itself prove that the recorded action was correct."),
    ("synthetic-appointment-coordination-v1.pdf", "Appointment Coordination Record", "appointment record source and status", "The synthetic coordination record tracks an appointment request source, requested service label, administrative status and owner. It contains no real date of birth or contact details.", "The queue owner updates status only from an authorized source. Missing information is sent for human follow-up using the local process.", "This fixture demonstrates record retrieval only; it does not schedule or confirm a real appointment."),
    ("synthetic-document-versioning-v1.pdf", "Document Version Control Register", "document version effective date and supersession metadata", "The example register records document name, version, effective date, review owner and superseded version. It helps distinguish sources without deciding which clinical guidance is current.", "Search and citation results preserve the version label. If two versions conflict, show both and escalate to the authorized policy owner.", "Never silently replace a cited source. Preserve the history needed to reproduce an evaluation run."),
    ("synthetic-quality-review-v1.pdf", "Quality Review Sampling Record", "quality review sample status and findings", "The fictional quality worksheet stores sample identifier, review criterion, reviewer status and source reference. It contains no performance claim about real clinicians.", "A reviewer marks each criterion pass, fail or not assessed and adds a source-linked explanation. An unassessed item must not be counted as a pass.", "Keep the reviewer's decision distinct from automated extraction or model suggestions."),
    ("synthetic-training-acknowledgement-v1.pdf", "Training Acknowledgement Record", "training version acknowledgement and completion status", "The synthetic register records training module version, acknowledgement status, timestamp and role. It avoids real names or employee identifiers.", "The workflow retains incomplete entries for human follow-up and records the exact training version associated with each acknowledgement.", "This file demonstrates version-aware search and must not be used as an employment record."),
    ("synthetic-data-correction-v1.pdf", "Source Data Correction Request", "correcting a source record with provenance", "The fictional correction request stores the original source reference, proposed correction, reason and review status. It does not overwrite the original source content.", "A reviewer verifies the requested change and records approve or reject with a timestamp. The previous and corrected versions remain attributable.", "Generated suggestions are untrusted until accepted by an authorized reviewer."),
    ("synthetic-retention-schedule-v1.pdf", "Assessment Artifact Retention Schedule", "retention status and deletion authorization fields", "The sample retention register records artifact category, retention label, review owner and authorization status. Its values apply only to these invented assessment files.", "A request is checked by the assigned owner before any deletion. The audit record retains the decision and scope of the approved operation.", "No real record-retention advice is provided. Follow current organizational policy for real information."),
]


def build_styles():
    base = getSampleStyleSheet()
    return {
        "title": ParagraphStyle("DocTitle", parent=base["Title"], fontName="Helvetica-Bold", fontSize=19,
                                leading=24, textColor=colors.HexColor("#12304A"), alignment=TA_LEFT, spaceAfter=15),
        "heading": ParagraphStyle("SectionTitle", parent=base["Heading2"], fontName="Helvetica-Bold", fontSize=13,
                                  leading=17, textColor=colors.HexColor("#176B6B"), spaceAfter=10),
        "body": ParagraphStyle("BodyTextCustom", parent=base["BodyText"], fontName="Helvetica", fontSize=10,
                                leading=15, alignment=TA_LEFT, spaceAfter=10),
        "small": ParagraphStyle("SmallText", parent=base["BodyText"], fontName="Helvetica", fontSize=9,
                                 leading=13, textColor=colors.HexColor("#425466"), spaceAfter=8),
    }


def source_version(filename):
    return "v2" if filename == "synthetic-follow-up-v2.pdf" else "v1"


def page_text(doc, page_number):
    title, purpose, workflow, record, escalation = doc[1:]
    shared_review = (
        f"For this assessment fixture, the review owner checks the source reference, section, version label, page number "
        f"and record status. Search results must preserve the source identity and quote only text present in the cited page. "
        f"If evidence is absent or conflicting, record the uncertainty and leave the conclusion for an authorized human reviewer. "
        f"This invented procedure exists to test ingestion, retrieval and citation traceability; it is not a care instruction."
    )
    if page_number == 1:
        return "Purpose and scope", [
            f"This synthetic assessment document covers {purpose}. It is a fictional workflow record used only to validate document ingestion, source attribution and retrieval behavior.",
            workflow,
            "The fixture is limited to fictional record-management examples. It does not establish a medical decision and is not validated for use outside this software assessment.",
        ]
    if page_number == 2:
        return "Workflow and ownership", [
            workflow,
            "The assigned owner first confirms the document name and version, then follows only the steps explicitly present in the approved source. If the source is incomplete, stop the affected administrative step and route the question to its named owner.",
            "Automated retrieval may locate passages but cannot grant permission, confirm a real-world fact or change an approval state. Human review remains responsible for decisions.",
        ]
    if page_number == 3:
        return "Record fields and provenance", [
            record,
            "For every entry, preserve the originating document, section, page, version and review date where available. Keep quoted source text separate from summaries, comments and reviewer conclusions.",
            "When information is missing, contradictory or unreadable, mark it as unknown or pending. Never fill a gap by guessing from nearby text or from model-generated content.",
        ]
    if page_number == 4:
        return "Uncertainty and escalation", [
            escalation,
            "Escalate unresolved conflicts to an authorized human owner. Include both source references and the exact conflicting passages so the reviewer can inspect them.",
            "A search score is a ranking signal, not proof. If the retrieved evidence does not support a requested statement, the valid result is to say that the source set does not establish it.",
        ]
    return "Version, review and limitations", [
        shared_review,
        "The source is a synthetic fixture with a stable filename and version. When another version is present, keep both versions visible and do not silently blend their contents.",
        "The text is deliberately fictional and is not a validated healthcare reference. Use it only for software testing and teaching, and describe demonstrations as engineering assessments.",
    ]


def draw_page(title, version):
    def draw(canvas, document):
        canvas.saveState()
        width, _ = letter
        canvas.setFont("Helvetica", 8)
        canvas.drawRightString(width - 0.75 * inch, 0.43 * inch,
                               f"{title} | {version} | Page {document.page} of {PAGE_COUNT}")
        canvas.setStrokeColor(colors.HexColor("#D5DEE7"))
        canvas.line(0.75 * inch, 0.58 * inch, width - 0.75 * inch, 0.58 * inch)
        canvas.restoreState()
    return draw


def create_pdf(doc, styles):
    filename, title, *_ = doc
    path = OUTPUT / filename
    version = source_version(filename)
    story = []
    for number in range(1, PAGE_COUNT + 1):
        section, paragraphs = page_text(doc, number)
        story.append(Paragraph(title if number == 1 else section, styles["title"] if number == 1 else styles["heading"]))
        story.append(Paragraph(f"Section {number}: {section} | Source version: {version}", styles["small"]))
        for text in paragraphs:
            story.append(Paragraph(text, styles["body"]))
        story.append(Spacer(1, 8))
        story.append(Paragraph(f"Training fixture only - document {filename} - page {number}.", styles["small"]))
        if number < PAGE_COUNT:
            story.append(PageBreak())

    pdf = SimpleDocTemplate(str(path), pagesize=letter, rightMargin=0.8 * inch, leftMargin=0.8 * inch,
                            topMargin=0.82 * inch, bottomMargin=0.78 * inch, title=title,
                            author="Domain Copilot synthetic assessment generator",
                            subject="Fictional workflow document; not clinical guidance")
    pdf.build(story, onFirstPage=draw_page(title, version), onLaterPages=draw_page(title, version),
              canvasmaker=partial(Canvas, invariant=1))
    return path


def main():
    if len(DOCUMENTS) != 30:
        raise SystemExit(f"Expected exactly 30 synthetic source documents, got {len(DOCUMENTS)}.")
    if len({item[0] for item in DOCUMENTS}) != len(DOCUMENTS):
        raise SystemExit("Synthetic source filenames must be unique.")

    OUTPUT.mkdir(parents=True, exist_ok=True)
    styles = build_styles()
    manifest = []
    for doc in DOCUMENTS:
        path = create_pdf(doc, styles)
        manifest.append({
            "fileName": path.name,
            "title": doc[1],
            "version": source_version(path.name),
            "expectedPages": PAGE_COUNT,
            "synthetic": True,
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        })
    MANIFEST.write_text(json.dumps({"corpus": "Domain Copilot fictional workflow fixtures", "documents": manifest,
                                    "documentCount": len(manifest), "expectedPageCount": PAGE_COUNT * len(manifest)},
                                   indent=2) + "\n", encoding="utf-8")
    print(f"Generated {len(manifest)} synthetic PDF documents with {PAGE_COUNT * len(manifest)} explicit pages.")
    print(f"Corpus directory: {OUTPUT}")
    print(f"Manifest: {MANIFEST}")


if __name__ == "__main__":
    main()
