import { z } from 'zod';
import { useTranslation } from 'react-i18next';
import { ReactNode } from 'react';

interface Props<TFormData> {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  form: any;
  name: keyof TFormData & string;
  schema: z.ZodSchema;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  children: (field: any) => ReactNode;
}

export function BgtFormField<TFormData>({ form, name, schema, children }: Props<TFormData>) {
  const { t } = useTranslation();

  return (
    <form.Field
      name={name}
      validators={{
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        onChange: ({ value }: { value: any }) => {
          const result = schema.safeParse(value);
          return result.success ? undefined : t(result.error.errors[0].message);
        },
      }}
    >
      {children}
    </form.Field>
  );
}
