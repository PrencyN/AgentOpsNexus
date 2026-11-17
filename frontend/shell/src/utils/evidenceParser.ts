export const EVIDENCE_ID_PATTERN =
  /\bDEP-EV-\d{3,}\b/g;

export interface EvidenceReference {
  id: string;
  sourceType: "mock-deployment";
  label: string;
}

export function extractEvidenceIds(
  content: string,
): string[] {
  if (!content.trim()) {
    return [];
  }

  const matches =
    content.match(EVIDENCE_ID_PATTERN) ?? [];

  return [...new Set(matches)];
}

export function createEvidenceReferences(
  content: string,
): EvidenceReference[] {
  return extractEvidenceIds(content).map(
    (id) => ({
      id,
      sourceType: "mock-deployment",
      label: `Deployment evidence ${id}`,
    }),
  );
}

export function transformEvidenceCitations(
  content: string,
): string {
  if (!content.trim()) {
    return content;
  }

  /*
   * Convert either:
   *
   * [DEP-EV-001]
   * DEP-EV-001
   *
   * into this Markdown link:
   *
   * #evidence-DEP-EV-001
   */
  return content.replace(
    /\[?(DEP-EV-\d{3,})\]?/g,
    (_match, evidenceId: string) => {
      return (
        "[" +
        evidenceId +
        "](#evidence-" +
        evidenceId +
        ")"
      );
    },
  );
}

export function isEvidenceHref(
  href: string | undefined,
): boolean {
  if (!href) {
    return false;
  }

  return /^#evidence-DEP-EV-\d{3,}$/.test(
    href,
  );
}

export function evidenceIdFromHref(
  href: string | undefined,
): string | null {
  if (!isEvidenceHref(href)) {
    return null;
  }

  return href!.replace("#evidence-", "");
}