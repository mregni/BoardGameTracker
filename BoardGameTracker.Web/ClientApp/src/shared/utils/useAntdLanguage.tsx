import {Locale} from 'antd/lib/locale';
import enUS from 'antd/locale/en_US';
import frFR from 'antd/locale/fr_FR';
import nlBE from 'antd/locale/nl_BE';
import nlNL from 'antd/locale/nl_NL';

export interface AntdLanguage {
  getLocale: (language: string) => Locale;
}

export const useAntdLanguage = (): AntdLanguage => {
  const getLocale = (language: string): Locale => {
    console.log(language)
    switch (language) {
      case 'nl':
      case 'nl-nl': return nlNL;
      case 'nl-be': return nlBE;
      case 'fr-fr': return frFR;
      default: return enUS;
    }
  };

  return { getLocale }
}