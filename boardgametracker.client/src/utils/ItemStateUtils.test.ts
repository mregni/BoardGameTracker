import { describe, it, expect } from 'vitest';

import { GameState } from '../models';

import {
  getItemStateTranslationKey,
  getItemStateTranslationKeyByString,
  getColorFromGameState,
} from './ItemStateUtils';

describe('ItemStateUtils', () => {
  describe('getItemStateTranslationKey', () => {
    describe('when item is loaned', () => {
      it('should return on-loan key regardless of game state', () => {
        expect(getItemStateTranslationKey(GameState.Wanted, true)).toBe('game.state.on-loan');
        expect(getItemStateTranslationKey(GameState.Owned, true)).toBe('game.state.on-loan');
        expect(getItemStateTranslationKey(GameState.PreviouslyOwned, true)).toBe('game.state.on-loan');
        expect(getItemStateTranslationKey(GameState.NotOwned, true)).toBe('game.state.on-loan');
        expect(getItemStateTranslationKey(GameState.ForTrade, true)).toBe('game.state.on-loan');
      });
    });

    describe('when item is not loaned', () => {
      it('should return wanted key for Wanted state', () => {
        expect(getItemStateTranslationKey(GameState.Wanted, false)).toBe('game.state.wanted');
      });

      it('should return owned key for Owned state', () => {
        expect(getItemStateTranslationKey(GameState.Owned, false)).toBe('game.state.owned');
      });

      it('should return previously-owned key for PreviouslyOwned state', () => {
        expect(getItemStateTranslationKey(GameState.PreviouslyOwned, false)).toBe('game.state.previously-owned');
      });

      it('should return not-owned key for NotOwned state', () => {
        expect(getItemStateTranslationKey(GameState.NotOwned, false)).toBe('game.state.not-owned');
      });

      it('should return for-trade key for ForTrade state', () => {
        expect(getItemStateTranslationKey(GameState.ForTrade, false)).toBe('game.state.for-trade');
      });

      it('should return empty string for unknown state', () => {
        expect(getItemStateTranslationKey(999 as GameState, false)).toBe('');
      });
    });
  });

  describe('getItemStateTranslationKeyByString', () => {
    it('should convert string to GameState and return translation key', () => {
      expect(getItemStateTranslationKeyByString('0')).toBe('game.state.wanted');
      expect(getItemStateTranslationKeyByString('1')).toBe('game.state.owned');
      expect(getItemStateTranslationKeyByString('2')).toBe('game.state.previously-owned');
      expect(getItemStateTranslationKeyByString('3')).toBe('game.state.not-owned');
      expect(getItemStateTranslationKeyByString('4')).toBe('game.state.for-trade');
    });

    it('should return empty string for invalid string values', () => {
      expect(getItemStateTranslationKeyByString('invalid')).toBe('');
      expect(getItemStateTranslationKeyByString('999')).toBe('');
    });
  });

  describe('getColorFromGameState', () => {
    describe('when item is loaned', () => {
      it('should return orange regardless of game state', () => {
        expect(getColorFromGameState(GameState.Wanted, true)).toBe('orange');
        expect(getColorFromGameState(GameState.Owned, true)).toBe('orange');
        expect(getColorFromGameState(GameState.PreviouslyOwned, true)).toBe('orange');
        expect(getColorFromGameState(GameState.NotOwned, true)).toBe('orange');
        expect(getColorFromGameState(GameState.ForTrade, true)).toBe('orange');
      });
    });

    describe('when item is not loaned', () => {
      it('should return amber for Wanted state', () => {
        expect(getColorFromGameState(GameState.Wanted, false)).toBe('amber');
      });

      it('should return green for Owned state', () => {
        expect(getColorFromGameState(GameState.Owned, false)).toBe('green');
      });

      it('should return red for PreviouslyOwned state', () => {
        expect(getColorFromGameState(GameState.PreviouslyOwned, false)).toBe('red');
      });

      it('should return purple for NotOwned state', () => {
        expect(getColorFromGameState(GameState.NotOwned, false)).toBe('purple');
      });

      it('should return blue for ForTrade state', () => {
        expect(getColorFromGameState(GameState.ForTrade, false)).toBe('blue');
      });

      it('should return orange for unknown state', () => {
        expect(getColorFromGameState(999 as GameState, false)).toBe('orange');
      });
    });

    it('should return valid color types', () => {
      const validColors = ['amber', 'orange', 'red', 'purple', 'blue', 'green'];
      const allStates = [
        GameState.Wanted,
        GameState.Owned,
        GameState.PreviouslyOwned,
        GameState.NotOwned,
        GameState.ForTrade,
      ];

      allStates.forEach((state) => {
        expect(validColors).toContain(getColorFromGameState(state, false));
        expect(validColors).toContain(getColorFromGameState(state, true));
      });
    });
  });
});
