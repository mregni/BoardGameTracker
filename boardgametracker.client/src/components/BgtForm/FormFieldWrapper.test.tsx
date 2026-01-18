import { describe, it, expect } from 'vitest';
import { render, screen } from '@/test/test-utils';

import { FormFieldWrapper } from './FormFieldWrapper';

describe('FormFieldWrapper', () => {
  describe('Rendering', () => {
    it('should render children', () => {
      render(
        <FormFieldWrapper>
          <input data-testid="child-input" />
        </FormFieldWrapper>
      );
      expect(screen.getByTestId('child-input')).toBeInTheDocument();
    });

    it('should render label when provided', () => {
      render(
        <FormFieldWrapper label="Username">
          <input />
        </FormFieldWrapper>
      );
      expect(screen.getByText('Username')).toBeInTheDocument();
    });

  });

  describe('Error Display', () => {
    it('should render errors when provided with label', () => {
      render(
        <FormFieldWrapper label="Email" errors={['Invalid email']}>
          <input />
        </FormFieldWrapper>
      );
      expect(screen.getByText('Invalid email')).toBeInTheDocument();
    });

    it('should render multiple errors', () => {
      render(
        <FormFieldWrapper label="Password" errors={['Too short', 'Needs uppercase']}>
          <input />
        </FormFieldWrapper>
      );
      expect(screen.getByText('Too short')).toBeInTheDocument();
      expect(screen.getByText('Needs uppercase')).toBeInTheDocument();
    });

    it('should not render errors when array is empty', () => {
      render(
        <FormFieldWrapper label="Name" errors={[]}>
          <input />
        </FormFieldWrapper>
      );
      expect(screen.queryByText('error')).not.toBeInTheDocument();
    });
  });

  describe('Combined Props', () => {
    it('should handle label, errors, and className together', () => {
      render(
        <FormFieldWrapper label="Field" errors={['Error']} className="my-class">
          <input data-testid="input" />
        </FormFieldWrapper>
      );

      expect(screen.getByText('Field')).toBeInTheDocument();
      expect(screen.getByText('Error')).toBeInTheDocument();
      expect(screen.getByTestId('input')).toBeInTheDocument();
    });
  });

  describe('Memo Behavior', () => {
    it('should be memoized', () => {
      // FormFieldWrapper is a memoized component
      expect(FormFieldWrapper.$$typeof?.toString()).toBe('Symbol(react.memo)');
    });
  });
});
