import * as locales from 'date-fns/locale';
import { Locale } from 'date-fns/locale';

export const getDateFnsLocaleKey = (languageCode: string): keyof typeof locales => {
  const mapping: Record<string, keyof typeof locales> = {
    'en-us': 'enUS',
    en: 'enUS',
    english: 'enUS',
    'nl-nl': 'nl',
    'nl-be': 'nl',
    nl: 'nl',
    dutch: 'nl',
    'fr-fr': 'fr',
    fr: 'fr',
    'de-de': 'de',
    de: 'de',
    'es-es': 'es',
    es: 'es',
    'it-it': 'it',
    it: 'it',
  };
  return mapping[languageCode.toLowerCase()] ?? 'enUS';
};

export const getDateFnsLocale = (languageCode: string): Locale => {
  const localeKey = getDateFnsLocaleKey(languageCode);
  return locales[localeKey];
};
