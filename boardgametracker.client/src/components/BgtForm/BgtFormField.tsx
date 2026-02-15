import type { z } from 'zod';
import { useTranslation } from 'react-i18next';
import type { ReactNode } from 'react';
import type { AnyFieldApi } from '@tanstack/react-form';

/* eslint-disable @typescript-eslint/no-explicit-any */
export interface AnyReactForm {
  Field: any;
  Subscribe: any;
  [key: string]: any;
}
/* eslint-enable @typescript-eslint/no-explicit-any */

interface Props<TSchema extends z.AnyZodObject> {
  form: AnyReactForm;
  name: keyof z.infer<TSchema> & string;
  schema: TSchema;
  children: (field: AnyFieldApi) => ReactNode;
}

export function BgtFormField<TSchema extends z.AnyZodObject>({ form, name, schema, children }: Props<TSchema>) {
  const { t } = useTranslation();
  const fieldSchema = schema.shape[name] as z.ZodSchema;

  return (
    <form.Field
      name={name}
      validators={{
        onChange: ({ value }: { value: unknown }) => {
          const result = fieldSchema.safeParse(value);
          return result.success ? undefined : t(result.error.errors[0].message);
        },
      }}
    >
      {children}
    </form.Field>
  );
}
