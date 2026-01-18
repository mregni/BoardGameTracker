import { describe, it, expect } from 'vitest';
import { render, screen } from '@/test/test-utils';

import { BgtFormErrors } from './BgtFormErrors';

describe('BgtFormErrors', () => {
  describe('Rendering', () => {
    it('should return null when errors is undefined', () => {
      const { container } = render(<BgtFormErrors />);
      expect(container.firstChild).toBeNull();
    });

    it('should return null when errors is empty array', () => {
      const { container } = render(<BgtFormErrors errors={[]} />);
      expect(container.firstChild).toBeNull();
    });

    it('should render single error', () => {
      render(<BgtFormErrors errors={['Field is required']} />);
      expect(screen.getByText('Field is required')).toBeInTheDocument();
    });

    it('should render multiple errors', () => {
      render(<BgtFormErrors errors={['Error 1', 'Error 2', 'Error 3']} />);
      expect(screen.getByText('Error 1')).toBeInTheDocument();
      expect(screen.getByText('Error 2')).toBeInTheDocument();
      expect(screen.getByText('Error 3')).toBeInTheDocument();
    });
  });

  describe('Edge Cases', () => {
    it('should handle errors with special characters', () => {
      render(<BgtFormErrors errors={['Value must be > 0 & < 100']} />);
      expect(screen.getByText('Value must be > 0 & < 100')).toBeInTheDocument();
    });

    it('should handle empty string errors', () => {
      render(<BgtFormErrors errors={['', 'Valid error']} />);
      expect(screen.getByText('Valid error')).toBeInTheDocument();
    });

    it('should handle long error messages', () => {
      const longError = 'This is a very long error message that exceeds normal length';
      render(<BgtFormErrors errors={[longError]} />);
      expect(screen.getByText(longError)).toBeInTheDocument();
    });
  });
});
