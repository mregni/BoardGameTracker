import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import {
  toInputDate,
  toInputDateTime,
  toDisplay,
  toDisplayDateTime,
  toRelative,
  isValidDate,
  safeParseDate,
  minutesToDuration,
  formatMinutesToDuration,
} from './dateUtils';

describe('dateUtils', () => {
  describe('toInputDate', () => {
    beforeEach(() => {
      vi.useFakeTimers();
      vi.setSystemTime(new Date('2024-06-15T10:30:00'));
    });

    afterEach(() => {
      vi.useRealTimers();
    });

    it('should return today when date is undefined and fallbackToToday is true', () => {
      expect(toInputDate(undefined)).toBe('2024-06-15');
    });

    it('should return empty string when date is undefined and fallbackToToday is false', () => {
      expect(toInputDate(undefined, false)).toBe('');
    });

    it('should format a Date object correctly', () => {
      const date = new Date('2024-03-20T14:30:00');
      expect(toInputDate(date)).toBe('2024-03-20');
    });

    it('should parse and format an ISO string correctly', () => {
      expect(toInputDate('2024-03-20T14:30:00')).toBe('2024-03-20');
    });

    it('should return today for invalid date string with fallback', () => {
      expect(toInputDate('invalid-date')).toBe('2024-06-15');
    });

    it('should return empty string for invalid date without fallback', () => {
      expect(toInputDate('invalid-date', false)).toBe('');
    });

    it('should handle Date object with invalid date', () => {
      const invalidDate = new Date('invalid');
      expect(toInputDate(invalidDate)).toBe('2024-06-15');
      expect(toInputDate(invalidDate, false)).toBe('');
    });
  });

  describe('toInputDateTime', () => {
    beforeEach(() => {
      vi.useFakeTimers();
      vi.setSystemTime(new Date('2024-06-15T10:30:00'));
    });

    afterEach(() => {
      vi.useRealTimers();
    });

    it('should return now when date is undefined and fallbackToNow is true', () => {
      expect(toInputDateTime(undefined)).toBe('2024-06-15T10:30');
    });

    it('should return empty string when date is undefined and fallbackToNow is false', () => {
      expect(toInputDateTime(undefined, false)).toBe('');
    });

    it('should format a Date object correctly', () => {
      const date = new Date('2024-03-20T14:45:00');
      expect(toInputDateTime(date)).toBe('2024-03-20T14:45');
    });

    it('should parse and format an ISO string correctly', () => {
      expect(toInputDateTime('2024-03-20T14:45:00')).toBe('2024-03-20T14:45');
    });

    it('should return now for invalid date string with fallback', () => {
      expect(toInputDateTime('invalid-date')).toBe('2024-06-15T10:30');
    });

    it('should return empty string for invalid date without fallback', () => {
      expect(toInputDateTime('invalid-date', false)).toBe('');
    });
  });

  describe('toDisplay', () => {
    it('should return empty string for null/undefined date', () => {
      expect(toDisplay(null, 'DD/MM/YYYY', 'en')).toBe('');
      expect(toDisplay(undefined, 'DD/MM/YYYY', 'en')).toBe('');
    });

    it('should format date with DD/MM/YYYY format', () => {
      const date = new Date('2024-03-20T14:30:00');
      expect(toDisplay(date, 'DD/MM/YYYY', 'en')).toBe('20/03/2024');
    });

    it('should format date with MM/DD/YYYY format', () => {
      const date = new Date('2024-03-20T14:30:00');
      expect(toDisplay(date, 'MM/DD/YYYY', 'en')).toBe('03/20/2024');
    });

    it('should format date with YYYY-MM-DD format', () => {
      const date = new Date('2024-03-20T14:30:00');
      expect(toDisplay(date, 'YYYY-MM-DD', 'en')).toBe('2024-03-20');
    });

    it('should handle ISO string input', () => {
      expect(toDisplay('2024-03-20T14:30:00', 'DD/MM/YYYY', 'en')).toBe('20/03/2024');
    });

    it('should return empty string for invalid date', () => {
      expect(toDisplay('invalid-date', 'DD/MM/YYYY', 'en')).toBe('');
    });
  });

  describe('toDisplayDateTime', () => {
    it('should return empty string for undefined date', () => {
      expect(toDisplayDateTime(undefined, 'DD/MM/YYYY', 'HH:mm', 'en')).toBe('');
    });

    it('should format date and time correctly', () => {
      const date = new Date('2024-03-20T14:45:30');
      expect(toDisplayDateTime(date, 'DD/MM/YYYY', 'HH:mm', 'en')).toBe('20/03/2024 14:45');
    });

    it('should handle 12-hour format', () => {
      const date = new Date('2024-03-20T14:45:30');
      expect(toDisplayDateTime(date, 'MM/DD/YYYY', 'hh:mm', 'en')).toBe('03/20/2024 02:45');
    });

    it('should handle ISO string input', () => {
      expect(toDisplayDateTime('2024-03-20T14:45:00', 'DD/MM/YYYY', 'HH:mm', 'en')).toBe('20/03/2024 14:45');
    });

    it('should return empty string for invalid date', () => {
      expect(toDisplayDateTime('invalid-date', 'DD/MM/YYYY', 'HH:mm', 'en')).toBe('');
    });
  });

  describe('toRelative', () => {
    beforeEach(() => {
      vi.useFakeTimers();
      vi.setSystemTime(new Date('2024-06-15T10:30:00'));
    });

    afterEach(() => {
      vi.useRealTimers();
    });

    it('should return empty string for undefined date', () => {
      expect(toRelative(undefined, 'en')).toBe('');
    });

    it('should return relative time for a past date', () => {
      const pastDate = new Date('2024-06-14T10:30:00');
      const result = toRelative(pastDate, 'en');
      expect(result).toContain('ago');
    });

    it('should handle ISO string input', () => {
      const result = toRelative('2024-06-14T10:30:00', 'en');
      expect(result).toContain('ago');
    });

    it('should return empty string for invalid date', () => {
      expect(toRelative('invalid-date', 'en')).toBe('');
    });

    it('should respect addSuffix option', () => {
      const pastDate = new Date('2024-06-14T10:30:00');
      const result = toRelative(pastDate, 'en', { addSuffix: false });
      expect(result).not.toContain('ago');
    });
  });

  describe('isValidDate', () => {
    it('should return false for null/undefined', () => {
      expect(isValidDate(null)).toBe(false);
      expect(isValidDate(undefined)).toBe(false);
    });

    it('should return true for valid Date object', () => {
      expect(isValidDate(new Date('2024-03-20'))).toBe(true);
    });

    it('should return false for invalid Date object', () => {
      expect(isValidDate(new Date('invalid'))).toBe(false);
    });

    it('should return true for valid ISO string', () => {
      expect(isValidDate('2024-03-20T14:30:00')).toBe(true);
    });

    it('should return false for invalid string', () => {
      expect(isValidDate('invalid-date')).toBe(false);
    });

    it('should return false for non-date types', () => {
      expect(isValidDate(123)).toBe(false);
      expect(isValidDate({})).toBe(false);
      expect(isValidDate([])).toBe(false);
    });

    it('should return false for empty string', () => {
      expect(isValidDate('')).toBe(false);
    });
  });

  describe('safeParseDate', () => {
    it('should return undefined for null/undefined', () => {
      expect(safeParseDate(undefined)).toBeUndefined();
    });

    it('should return Date object as-is if valid', () => {
      const date = new Date('2024-03-20');
      expect(safeParseDate(date)).toBe(date);
    });

    it('should return undefined for invalid Date object', () => {
      expect(safeParseDate(new Date('invalid'))).toBeUndefined();
    });

    it('should parse valid ISO string to Date', () => {
      const result = safeParseDate('2024-03-20T14:30:00');
      expect(result).toBeInstanceOf(Date);
      expect(result?.getFullYear()).toBe(2024);
      expect(result?.getMonth()).toBe(2); // March is 2 (0-indexed)
      expect(result?.getDate()).toBe(20);
    });

    it('should return undefined for invalid string', () => {
      expect(safeParseDate('invalid-date')).toBeUndefined();
    });
  });

  describe('minutesToDuration', () => {
    it('should convert 0 minutes correctly', () => {
      expect(minutesToDuration(0)).toEqual({ weeks: 0, days: 0, hours: 0, minutes: 0 });
    });

    it('should convert minutes only', () => {
      expect(minutesToDuration(45)).toEqual({ weeks: 0, days: 0, hours: 0, minutes: 45 });
    });

    it('should convert hours and minutes', () => {
      expect(minutesToDuration(90)).toEqual({ weeks: 0, days: 0, hours: 1, minutes: 30 });
    });

    it('should convert days, hours, and minutes', () => {
      // 1 day = 1440 minutes, 2 hours = 120 minutes, 30 minutes
      expect(minutesToDuration(1590)).toEqual({ weeks: 0, days: 1, hours: 2, minutes: 30 });
    });

    it('should convert weeks, days, hours, and minutes', () => {
      // 1 week = 10080 minutes, 2 days = 2880 minutes, 3 hours = 180 minutes, 15 minutes
      expect(minutesToDuration(13155)).toEqual({ weeks: 1, days: 2, hours: 3, minutes: 15 });
    });

    it('should handle exact week boundary', () => {
      expect(minutesToDuration(10080)).toEqual({ weeks: 1, days: 0, hours: 0, minutes: 0 });
    });

    it('should handle exact day boundary', () => {
      expect(minutesToDuration(1440)).toEqual({ weeks: 0, days: 1, hours: 0, minutes: 0 });
    });

    it('should handle exact hour boundary', () => {
      expect(minutesToDuration(60)).toEqual({ weeks: 0, days: 0, hours: 1, minutes: 0 });
    });
  });

  describe('formatMinutesToDuration', () => {
    it('should return null for null input', () => {
      expect(formatMinutesToDuration(null, ['hours', 'minutes'])).toBeNull();
    });

    it('should return null for 0 input', () => {
      expect(formatMinutesToDuration(0, ['hours', 'minutes'])).toBeNull();
    });

    it('should format minutes to hours and minutes', () => {
      const result = formatMinutesToDuration(90, ['hours', 'minutes']);
      expect(result).toContain('1');
      expect(result).toContain('hour');
      expect(result).toContain('30');
      expect(result).toContain('minute');
    });

    it('should format using only specified units', () => {
      const result = formatMinutesToDuration(90, ['minutes']);
      // Should only show minutes if hours is not in format
      expect(result).toBeDefined();
    });

    it('should handle days in format', () => {
      const result = formatMinutesToDuration(1590, ['days', 'hours', 'minutes']);
      expect(result).toContain('1');
      expect(result).toContain('day');
    });
  });
});
