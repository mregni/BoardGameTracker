import { z } from 'zod';

export interface Settings {
  dateFormat: string;
  timeFormat: string;
  uiLanguage: string;
  currency: string;
  decimalSeparator: string;
  statistics: boolean;
}

export const SettingsSchema = z.object({
  dateFormat: z
    .string({
      required_error: 'settings.date-format.required',
    })
    .min(1, {
      message: 'settings.date-format.required',
    }),
  timeFormat: z
    .string({
      required_error: 'settings.time-format.required',
    })
    .min(1, {
      message: 'settings.time-format.required',
    }),
  uiLanguage: z.string({
    required_error: 'settings.ui-language.required',
  }),
  currency: z
    .string({
      required_error: 'settings.currency.required',
    })
    .min(1, {
      message: 'settings.currency.required',
    }),
  decimalSeparator: z
    .string({
      required_error: 'settings.decimal-separator.required',
    })

    .min(1, {
      message: 'settings.decimal-separator.required',
    })
    .max(1, {
      message: 'settings.decimal-separator.max-length',
    }),
});
