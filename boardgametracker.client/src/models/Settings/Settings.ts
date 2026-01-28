import { z } from 'zod';

export interface Settings {
  dateFormat: string;
  timeFormat: string;
  uiLanguage: string;
  currency: string;
  statistics: boolean;
  updateCheckEnabled: boolean;
  versionTrack: string;
  shelfOfShameEnabled: boolean;
  shelfOfShameMonthsLimit: number;
  oidcEnabled: boolean;
  oidcProvider: string;
  oidcClientId: string;
  oidcClientSecret: string;
  publicUrl: string;
  primaryColor: string;
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
  statistics: z.boolean(),
  updateCheckEnabled: z.boolean(),
  versionTrack: z.string(),
  shelfOfShameEnabled: z.boolean(),
  shelfOfShameMonthsLimit: z.number().min(1),
  oidcEnabled: z.boolean(),
  oidcProvider: z.string(),
  oidcClientId: z.string(),
  oidcClientSecret: z.string(),
  publicUrl: z.string(),
  primaryColor: z.string(),
});
