import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import {
  evidenceIdFromHref,
  transformEvidenceCitations,
} from "../utils/evidenceParser";
import { EvidenceBadge } from "./EvidenceBadge";

interface MarkdownResponseProps {
  content: string;
}

export function MarkdownResponse({
  content,
}: MarkdownResponseProps) {
  if (!content.trim()) {
    return null;
  }

  const transformedContent =
    transformEvidenceCitations(content);

  return (
    <div
      className="markdown-response"
      aria-live="polite"
    >
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        components={{
          h1: ({ children }) => (
            <h2 className="markdown-heading-primary">
              {children}
            </h2>
          ),

          h2: ({ children }) => (
            <h3 className="markdown-heading-secondary">
              {children}
            </h3>
          ),

          h3: ({ children }) => (
            <h4 className="markdown-heading-tertiary">
              {children}
            </h4>
          ),

          p: ({ children }) => (
            <p className="markdown-paragraph">
              {children}
            </p>
          ),

          ul: ({ children }) => (
            <ul className="markdown-list">
              {children}
            </ul>
          ),

          ol: ({ children }) => (
            <ol className="markdown-list markdown-numbered-list">
              {children}
            </ol>
          ),

          li: ({ children }) => (
            <li className="markdown-list-item">
              {children}
            </li>
          ),

          blockquote: ({ children }) => (
            <blockquote className="markdown-blockquote">
              {children}
            </blockquote>
          ),

          code: ({
            children,
            className,
          }) => {
            if (className) {
              return (
                <code className={className}>
                  {children}
                </code>
              );
            }

            return (
              <code className="markdown-inline-code">
                {children}
              </code>
            );
          },

          pre: ({ children }) => (
            <pre className="markdown-code-block">
              {children}
            </pre>
          ),

          table: ({ children }) => (
            <div className="markdown-table-wrapper">
              <table className="markdown-table">
                {children}
              </table>
            </div>
          ),

          th: ({ children }) => (
            <th scope="col">{children}</th>
          ),

          a: ({
            href,
            children,
          }) => {
            const evidenceId =
              evidenceIdFromHref(href);

            if (evidenceId) {
              return (
                <EvidenceBadge
                  evidenceId={evidenceId}
                />
              );
            }

            return (
              <a
                href={href}
                target="_blank"
                rel="noreferrer noopener"
              >
                {children}
              </a>
            );
          },
        }}
      >
        {transformedContent}
      </ReactMarkdown>
    </div>
  );
}