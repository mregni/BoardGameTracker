import { format, formatDistanceToNow, isValid as dateFnsIsValid, parseISO, formatDuration } from 'date-fns';

import { getDateFnsLocale } from './localeUtils';

const convertDateFormat = (userFormat: string): string => {
  return userFormat.replace(/YYYY/g, 'yyyy').replace(/DD/g, 'dd').replace(/MM/g, 'MM');
};

const convertTimeFormat = (userFormat: string): string => {
  return userFormat.replace(/HH/g, 'HH').replace(/hh/g, 'hh').replace(/mm/g, 'mm').replace(/ss/g, 'ss');
};

export const toInputDate = (date: Date | string | undefined, fallbackToToday = true): string => {
  if (!date && fallbackToToday) {
    return format(new Date(), 'yyyy-MM-dd');
  }

  if (!date) {
    return '';
  }

  const dateObj = typeof date === 'string' ? parseISO(date) : date;

  if (!dateFnsIsValid(dateObj)) {
    return fallbackToToday ? format(new Date(), 'yyyy-MM-dd') : '';
  }

  return format(dateObj, 'yyyy-MM-dd');
};

export const toInputDateTime = (date: Date | string | undefined, fallbackToNow = true): string => {
  if (!date && fallbackToNow) {
    return format(new Date(), "yyyy-MM-dd'T'HH:mm");
  }

  if (!date) {
    return '';
  }

  const dateObj = typeof date === 'string' ? parseISO(date) : date;

  if (!dateFnsIsValid(dateObj)) {
    return fallbackToNow ? format(new Date(), "yyyy-MM-dd'T'HH:mm") : '';
  }

  return format(dateObj, "yyyy-MM-dd'T'HH:mm");
};

export const toDisplay = (date: Date | string | undefined | null, dateFormat: string, uiLanguage: string): string => {
  if (!date) {
    return '';
  }

  const dateObj = typeof date === 'string' ? parseISO(date) : date;

  if (!dateFnsIsValid(dateObj)) {
    return '';
  }

  const locale = getDateFnsLocale(uiLanguage);
  const dateFnsFormat = convertDateFormat(dateFormat);
  return format(dateObj, dateFnsFormat, { locale });
};

export const toDisplayDateTime = (
  date: Date | string | undefined,
  dateFormat: string,
  timeFormat: string,
  uiLanguage: string
): string => {
  if (!date) {
    return '';
  }

  const dateObj = typeof date === 'string' ? parseISO(date) : date;

  if (!dateFnsIsValid(dateObj)) {
    return '';
  }

  const locale = getDateFnsLocale(uiLanguage);
  const dateFnsDateFormat = convertDateFormat(dateFormat);
  const dateFnsTimeFormat = convertTimeFormat(timeFormat);
  const combinedFormat = `${dateFnsDateFormat} ${dateFnsTimeFormat}`;

  return format(dateObj, combinedFormat, { locale });
};

export const toRelative = (
  date: Date | string | undefined,
  uiLanguage: string,
  options?: { addSuffix?: boolean; includeSeconds?: boolean }
): string => {
  if (!date) {
    return '';
  }

  const dateObj = typeof date === 'string' ? parseISO(date) : date;

  if (!dateFnsIsValid(dateObj)) {
    return '';
  }

  const locale = getDateFnsLocale(uiLanguage);
  return formatDistanceToNow(dateObj, {
    locale,
    addSuffix: true,
    ...options,
  });
};

export const isValidDate = (date: unknown): boolean => {
  if (!date) {
    return false;
  }

  if (date instanceof Date) {
    return dateFnsIsValid(date);
  }

  if (typeof date === 'string') {
    const parsed = parseISO(date);
    return dateFnsIsValid(parsed);
  }

  return false;
};

export const safeParseDate = (date: Date | string | undefined): Date | undefined => {
  if (!date) {
    return undefined;
  }

  if (date instanceof Date) {
    return dateFnsIsValid(date) ? date : undefined;
  }

  const parsed = parseISO(date);
  return dateFnsIsValid(parsed) ? parsed : undefined;
};

export const minutesToDuration = (
  totalMinutes: number
): { weeks: number; days: number; hours: number; minutes: number } => {
  const MINUTES_PER_WEEK = 7 * 24 * 60;
  const MINUTES_PER_DAY = 24 * 60;
  const MINUTES_PER_HOUR = 60;

  const weeks = Math.floor(totalMinutes / MINUTES_PER_WEEK);
  const remainingAfterWeeks = totalMinutes % MINUTES_PER_WEEK;

  const days = Math.floor(remainingAfterWeeks / MINUTES_PER_DAY);
  const remainingAfterDays = remainingAfterWeeks % MINUTES_PER_DAY;

  const hours = Math.floor(remainingAfterDays / MINUTES_PER_HOUR);
  const minutes = remainingAfterDays % MINUTES_PER_HOUR;

  return { weeks, days, hours, minutes };
};

export const formatMinutesToDuration = (
  minutes: number | null,
  formatUnits: Array<'months' | 'weeks' | 'days' | 'hours' | 'minutes' | 'seconds'>,
  uiLanguage?: string
): string | null => {
  if (!minutes) return null;

  const duration = minutesToDuration(minutes);
  const locale = uiLanguage ? getDateFnsLocale(uiLanguage) : undefined;

  return formatDuration(duration, {
    format: formatUnits,
    locale,
  });
};
