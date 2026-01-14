import { z } from 'zod';
import { useTranslation } from 'react-i18next';

export const useFormValidation = () => {
  const { t } = useTranslation();

  return {
    createValidator: <T>(schema: z.ZodSchema<T>) => ({
      onChange: ({ value }: { value: unknown }) => {
        const result = schema.safeParse(value);
        return result.success ? undefined : t(result.error.errors[0].message);
      },
    }),
  };
};
