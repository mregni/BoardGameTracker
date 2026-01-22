import { describe, it, expect } from 'vitest';

import { BgtMenuLogo } from './BgtMenuLogo';

import { screen, renderWithTheme } from '@/test/test-utils';

describe('BgtMenuLogo', () => {
  describe('Rendering', () => {
    it('should render logo text', () => {
      renderWithTheme(<BgtMenuLogo />);
      expect(screen.getByText('Board games')).toBeInTheDocument();
    });
  });
});
