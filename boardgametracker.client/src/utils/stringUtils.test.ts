import { describe, it, expect } from 'vitest';
import { StringToHsl, StringToRgb, SessionFlagToString } from './stringUtils';
import { SessionFlag } from '@/models';

describe('stringUtils', () => {
  describe('StringToHsl', () => {
    it('should return default red hue for undefined input', () => {
      expect(StringToHsl(undefined)).toBe('hsl(0, 85%, 35%)');
    });

    it('should return consistent HSL for the same string', () => {
      const result1 = StringToHsl('test');
      const result2 = StringToHsl('test');
      expect(result1).toBe(result2);
    });

    it('should return different HSL for different strings', () => {
      const result1 = StringToHsl('hello');
      const result2 = StringToHsl('world');
      expect(result1).not.toBe(result2);
    });

    it('should return valid HSL format', () => {
      const result = StringToHsl('test');
      expect(result).toMatch(/^hsl\(\d+, 85%, 35%\)$/);
    });

    it('should handle empty string', () => {
      const result = StringToHsl('');
      expect(result).toBe('hsl(0, 85%, 35%)');
    });

    it('should handle special characters', () => {
      const result = StringToHsl('test@#$%');
      expect(result).toMatch(/^hsl\(\d+, 85%, 35%\)$/);
    });

    it('should handle unicode characters', () => {
      const result = StringToHsl('测试');
      expect(result).toMatch(/^hsl\(\d+, 85%, 35%\)$/);
    });

    it('should handle very long strings', () => {
      const longString = 'a'.repeat(1000);
      const result = StringToHsl(longString);
      expect(result).toMatch(/^hsl\(\d+, 85%, 35%\)$/);
    });
  });

  describe('StringToRgb', () => {
    it('should return black for undefined input', () => {
      expect(StringToRgb(undefined)).toBe('#000000');
    });

    it('should return consistent RGB for the same string', () => {
      const result1 = StringToRgb('test');
      const result2 = StringToRgb('test');
      expect(result1).toBe(result2);
    });

    it('should return different RGB for different strings', () => {
      const result1 = StringToRgb('hello');
      const result2 = StringToRgb('world');
      expect(result1).not.toBe(result2);
    });

    it('should return valid RGB format', () => {
      const result = StringToRgb('test');
      expect(result).toMatch(/^rgb\(\d+, \d+, \d+\)$/);
    });

    it('should return RGB values within valid range (0-255)', () => {
      const result = StringToRgb('test');
      const match = result.match(/rgb\((\d+), (\d+), (\d+)\)/);
      expect(match).not.toBeNull();
      if (match) {
        const r = parseInt(match[1], 10);
        const g = parseInt(match[2], 10);
        const b = parseInt(match[3], 10);
        expect(r).toBeGreaterThanOrEqual(0);
        expect(r).toBeLessThanOrEqual(255);
        expect(g).toBeGreaterThanOrEqual(0);
        expect(g).toBeLessThanOrEqual(255);
        expect(b).toBeGreaterThanOrEqual(0);
        expect(b).toBeLessThanOrEqual(255);
      }
    });

    it('should handle empty string', () => {
      const result = StringToRgb('');
      expect(result).toMatch(/^rgb\(\d+, \d+, \d+\)$/);
    });

    it('should handle special characters', () => {
      const result = StringToRgb('test@#$%');
      expect(result).toMatch(/^rgb\(\d+, \d+, \d+\)$/);
    });
  });

  describe('SessionFlagToString', () => {
    it('should return correct key for LongestGame', () => {
      expect(SessionFlagToString(SessionFlag.LongestGame)).toBe('common.flags.longest-game');
    });

    it('should return correct key for ShortestGame', () => {
      expect(SessionFlagToString(SessionFlag.ShortestGame)).toBe('common.flags.shortest-game');
    });

    it('should return correct key for HighestScore', () => {
      expect(SessionFlagToString(SessionFlag.HighestScore)).toBe('common.flags.highest-score');
    });

    it('should return correct key for LowestScore', () => {
      expect(SessionFlagToString(SessionFlag.LowestScore)).toBe('common.flags.lowest-score');
    });

    it('should handle all enum values', () => {
      const allFlags = [
        SessionFlag.LongestGame,
        SessionFlag.ShortestGame,
        SessionFlag.HighestScore,
        SessionFlag.LowestScore,
      ];

      allFlags.forEach((flag) => {
        const result = SessionFlagToString(flag);
        expect(result).toMatch(/^common\.flags\./);
      });
    });
  });
});
