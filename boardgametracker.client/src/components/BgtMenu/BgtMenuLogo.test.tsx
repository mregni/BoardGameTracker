import { describe, it, expect } from 'vitest';
import { screen, renderWithTheme } from '@/test/test-utils';

import { BgtMenuLogo } from './BgtMenuLogo';

describe('BgtMenuLogo', () => {
  describe('Rendering', () => {
    it('should render logo text', () => {
      renderWithTheme(<BgtMenuLogo />);
      expect(screen.getByText('Board games')).toBeInTheDocument();
    });
  });
});
