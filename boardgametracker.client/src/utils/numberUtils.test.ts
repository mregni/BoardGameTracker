import { describe, it, expect } from 'vitest';

import { RoundDecimal, GetPercentage, ToLogLevel } from './numberUtils';

describe('numberUtils', () => {
  describe('RoundDecimal', () => {
    it('should return null for null input', () => {
      expect(RoundDecimal(null)).toBeNull();
    });

    it('should round to nearest integer by default', () => {
      expect(RoundDecimal(4.5)).toBe(5);
      expect(RoundDecimal(4.4)).toBe(4);
      expect(RoundDecimal(4.6)).toBe(5);
    });

    it('should round to specified increment', () => {
      expect(RoundDecimal(7, 5)).toBe(5);
      expect(RoundDecimal(8, 5)).toBe(10);
      expect(RoundDecimal(12, 5)).toBe(10);
      expect(RoundDecimal(13, 5)).toBe(15);
    });

    it('should round to decimal increments', () => {
      expect(RoundDecimal(4.27, 0.5)).toBe(4.5);
      expect(RoundDecimal(4.24, 0.5)).toBe(4);
      expect(RoundDecimal(4.75, 0.5)).toBe(5);
    });

    it('should handle zero', () => {
      expect(RoundDecimal(0)).toBe(0);
      expect(RoundDecimal(0, 5)).toBe(0);
    });

    it('should handle negative numbers', () => {
      expect(RoundDecimal(-4.5)).toBe(-4);
      expect(RoundDecimal(-4.6)).toBe(-5);
      expect(RoundDecimal(-7, 5)).toBe(-5);
    });

    it('should handle large numbers', () => {
      expect(RoundDecimal(1234567.89)).toBe(1234568);
      expect(RoundDecimal(1234567, 100)).toBe(1234600);
    });
  });

  describe('GetPercentage', () => {
    it('should return 0 when total is 0', () => {
      expect(GetPercentage(5, 0)).toBe(0);
      expect(GetPercentage(0, 0)).toBe(0);
    });

    it('should calculate correct percentage', () => {
      expect(GetPercentage(25, 100)).toBe(25);
      expect(GetPercentage(1, 4)).toBe(25);
      expect(GetPercentage(1, 3)).toBe(33);
    });

    it('should round to nearest integer', () => {
      expect(GetPercentage(1, 3)).toBe(33);
      expect(GetPercentage(2, 3)).toBe(67);
    });

    it('should handle 100%', () => {
      expect(GetPercentage(100, 100)).toBe(100);
      expect(GetPercentage(5, 5)).toBe(100);
    });

    it('should handle 0%', () => {
      expect(GetPercentage(0, 100)).toBe(0);
      expect(GetPercentage(0, 5)).toBe(0);
    });

    it('should handle values greater than total', () => {
      expect(GetPercentage(150, 100)).toBe(150);
      expect(GetPercentage(10, 5)).toBe(200);
    });

    it('should handle decimal values', () => {
      expect(GetPercentage(0.5, 1)).toBe(50);
      expect(GetPercentage(0.333, 1)).toBe(33);
    });
  });

  describe('ToLogLevel', () => {
    it('should return warn for level 0', () => {
      expect(ToLogLevel(0)).toBe('log-levels.warn');
    });

    it('should return debug for level 1', () => {
      expect(ToLogLevel(1)).toBe('log-levels.debug');
    });

    it('should return info for level 2', () => {
      expect(ToLogLevel(2)).toBe('log-levels.info');
    });

    it('should return warn for level 3', () => {
      expect(ToLogLevel(3)).toBe('log-levels.warn');
    });

    it('should return error for level 4', () => {
      expect(ToLogLevel(4)).toBe('log-levels.error');
    });

    it('should return warn for out of range levels', () => {
      expect(ToLogLevel(5)).toBe('log-levels.warn');
      expect(ToLogLevel(100)).toBe('log-levels.warn');
      expect(ToLogLevel(-1)).toBe('log-levels.warn');
    });
  });
});
