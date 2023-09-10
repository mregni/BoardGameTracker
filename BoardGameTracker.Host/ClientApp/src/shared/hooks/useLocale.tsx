import {eachDayOfInterval, endOfWeek, format, Locale, startOfWeek} from 'date-fns';
import enUs from 'date-fns/locale/en-US';
import nl from 'date-fns/locale/nl';
import {useContext, useEffect, useState} from 'react';

import {SettingsContext} from '../context/settingsContext';

interface Props {
  locale: Locale;
}

export const useLocale = (): Props => {
  const {settings} = useContext(SettingsContext);
  const [locale, setLocale] = useState<Locale>(null!);

  useEffect(() => {
    if (settings !== null) {
      setLocale(getLocale(settings.uiLanguage));
    }
  }, [settings]);

  const getLocale = (language: string): Locale => {
    switch (language) {
    case 'nl':
    case 'nl-NL':
    case 'nl-BE': return nl;
    case 'en-US':
    default: return enUs;
    }
  };

  return {locale};
};
