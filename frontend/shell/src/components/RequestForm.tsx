import { zodResolver } from "@hookform/resolvers/zod";
import {
  Button,
  Checkbox,
  Field,
  Select,
  Textarea,
} from "@fluentui/react-components";
import { useForm } from "react-hook-form";
import { z } from "zod";
import type { CreateAgentRequest } from "../types/agent";

const requestSchema = z.object({
  message: z
    .string()
    .trim()
    .min(10, "Enter at least 10 characters.")
    .max(4000, "The request cannot exceed 4,000 characters."),
  environment: z.enum([
    "Development",
    "Test",
    "Staging",
    "Production",
  ]),
  severity: z.enum([
    "Low",
    "Medium",
    "High",
    "Critical",
  ]),
  simulateFailure: z.boolean(),
});

interface RequestFormProps {
  busy: boolean;
  onSubmit: (request: CreateAgentRequest) => Promise<void>;
  onCancel: () => void;
}

export function RequestForm({
  busy,
  onSubmit,
  onCancel,
}: RequestFormProps) {
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<CreateAgentRequest>({
    resolver: zodResolver(requestSchema),
    defaultValues: {
      message:
        "Checkout failures increased after the latest PaymentService deployment.",
      environment: "Development",
      severity: "High",
      simulateFailure: false,
    },
  });

  const message = watch("message") ?? "";

  return (
    <form
      className="request-form"
      onSubmit={handleSubmit(onSubmit)}
      noValidate
    >
      <Field
        label="Incident description"
        validationMessage={errors.message?.message}
        validationState={errors.message ? "error" : "none"}
        hint={`${message.length}/4000 characters`}
        required
      >
        <Textarea
          {...register("message")}
          resize="vertical"
          rows={7}
          placeholder="Describe the production incident..."
          disabled={busy}
          aria-label="Incident description"
        />
      </Field>

      <div className="form-row">
        <Field
          label="Environment"
          validationMessage={errors.environment?.message}
          validationState={
            errors.environment ? "error" : "none"
          }
          required
        >
          <Select
            {...register("environment")}
            disabled={busy}
            aria-label="Environment"
          >
            <option value="Development">Development</option>
            <option value="Test">Test</option>
            <option value="Staging">Staging</option>
            <option value="Production">Production</option>
          </Select>
        </Field>

        <Field
          label="Severity"
          validationMessage={errors.severity?.message}
          validationState={errors.severity ? "error" : "none"}
          required
        >
          <Select
            {...register("severity")}
            disabled={busy}
            aria-label="Severity"
          >
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="High">High</option>
            <option value="Critical">Critical</option>
          </Select>
        </Field>
      </div>

      <Checkbox
        {...register("simulateFailure")}
        label="Simulate AI service failure"
        disabled={busy}
      />

      <div className="form-actions">
        <Button
          appearance="primary"
          type="submit"
          disabled={busy}
        >
          {busy ? "Investigation running..." : "Start investigation"}
        </Button>

        {busy && (
          <Button
            type="button"
            appearance="secondary"
            onClick={onCancel}
          >
            Cancel
          </Button>
        )}
      </div>
    </form>
  );
}