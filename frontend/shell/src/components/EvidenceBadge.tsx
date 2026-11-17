import {
  Badge,
  Tooltip,
} from "@fluentui/react-components";

interface EvidenceBadgeProps {
  evidenceId: string;
  onSelect?: (evidenceId: string) => void;
}

export function EvidenceBadge({
  evidenceId,
  onSelect,
}: EvidenceBadgeProps) {
  const handleSelect = () => {
    const evidenceElement =
      document.getElementById(
        `evidence-${evidenceId}`,
      );

    evidenceElement?.scrollIntoView?.({
      behavior: "smooth",
      block: "center",
    });

    evidenceElement?.focus();

    onSelect?.(evidenceId);
  };

  return (
    <Tooltip
      content={`Open details for ${evidenceId}`}
      relationship="description"
    >
      <button
        type="button"
        className="evidence-badge-button"
        onClick={handleSelect}
        aria-label={`Open evidence ${evidenceId}`}
      >
        <Badge
          appearance="tint"
          color="brand"
          size="small"
        >
          {evidenceId}
        </Badge>
      </button>
    </Tooltip>
  );
}