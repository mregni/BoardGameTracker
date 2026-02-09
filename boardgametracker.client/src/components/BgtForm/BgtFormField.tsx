import { z } from 'zod';
import { useTranslation } from 'react-i18next';
import { ReactNode } from 'react';

interface Props<TSchema extends z.AnyZodObject> {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  form: any;
  name: keyof z.infer<TSchema> & string;
  schema: TSchema;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  children: (field: any) => ReactNode;
}

export function BgtFormField<TSchema extends z.AnyZodObject>({ form, name, schema, children }: Props<TSchema>) {
  const { t } = useTranslation();
  const fieldSchema = schema.shape[name] as z.ZodSchema;

  return (
    <form.Field
      name={name}
      validators={{
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        onChange: ({ value }: { value: any }) => {
          const result = fieldSchema.safeParse(value);
          return result.success ? undefined : t(result.error.errors[0].message);
        },
      }}
    >
      {children}
    </form.Field>
  );
}
