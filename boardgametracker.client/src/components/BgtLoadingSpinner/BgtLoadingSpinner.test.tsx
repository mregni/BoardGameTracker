import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@/test/test-utils';

import { BgtLoadingSpinner } from './BgtLoadingSpinner';

vi.mock('react-loading-icons', () => ({
  Bars: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="loading-bars" {...props} />,
}));

describe('BgtLoadingSpinner', () => {
  describe('Rendering', () => {
    it('should render the loading icon', () => {
      render(<BgtLoadingSpinner />);
      expect(screen.getByTestId('loading-bars')).toBeInTheDocument();
    });
  });
});
