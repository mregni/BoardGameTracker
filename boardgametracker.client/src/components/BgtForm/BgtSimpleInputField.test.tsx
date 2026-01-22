import { describe, it, expect, vi } from 'vitest';

import { BgtSimpleInputField } from './BgtSimpleInputField';

import { screen, userEvent, renderWithTheme } from '@/test/test-utils';


describe('BgtSimpleInputField', () => {
  const defaultProps = {
    type: 'text' as const,
    value: '',
    onChange: vi.fn(),
  };

  describe('Rendering', () => {
    it('should render input element', () => {
      renderWithTheme(<BgtSimpleInputField {...defaultProps} />);
      const input = screen.getByRole('textbox');
      expect(input).toBeInTheDocument();
    });

    it('should render label when provided', () => {
      renderWithTheme(<BgtSimpleInputField {...defaultProps} label="Test Label" />);
      expect(screen.getByText('Test Label')).toBeInTheDocument();
    });

    it('should not render label when not provided', () => {
      renderWithTheme(<BgtSimpleInputField {...defaultProps} />);
      expect(screen.queryByText('Test Label')).not.toBeInTheDocument();
    });
  });

  describe('Input Types', () => {
    it('should render text input', () => {
      renderWithTheme(<BgtSimpleInputField {...defaultProps} type="text" />);
      const input = screen.getByRole('textbox');
      expect(input).toHaveAttribute('type', 'text');
    });

    it('should render number input', () => {
      renderWithTheme(<BgtSimpleInputField {...defaultProps} type="number" />);
      const input = screen.getByRole('spinbutton');
      expect(input).toHaveAttribute('type', 'number');
    });

    it('should render date input', () => {
      const { container } = renderWithTheme(<BgtSimpleInputField {...defaultProps} type="date" />);
      const input = container.querySelector('input[type="date"]');
      expect(input).toBeInTheDocument();
    });
  });

  describe('Value Handling', () => {
    it('should display string value', () => {
      renderWithTheme(<BgtSimpleInputField {...defaultProps} value="test value" />);
      const input = screen.getByRole('textbox');
      expect(input).toHaveValue('test value');
    });

    it('should display number value', () => {
      renderWithTheme(<BgtSimpleInputField {...defaultProps} type="number" value={42} />);
      const input = screen.getByRole('spinbutton');
      expect(input).toHaveValue(42);
    });

    it('should format Date value to yyyy-MM-dd', () => {
      const date = new Date('2024-03-15');
      const { container } = renderWithTheme(<BgtSimpleInputField {...defaultProps} type="date" value={date} />);
      const input = container.querySelector('input') as HTMLInputElement;
      expect(input.value).toBe('2024-03-15');
    });

    it('should handle undefined value', () => {
      renderWithTheme(<BgtSimpleInputField {...defaultProps} value={undefined} />);
      const input = screen.getByRole('textbox');
      expect(input).toHaveValue('');
    });

    it('should handle null value as empty string', () => {
      renderWithTheme(<BgtSimpleInputField {...defaultProps} value={null as unknown as undefined} />);
      const input = screen.getByRole('textbox');
      expect(input).toHaveValue('');
    });
  });

  describe('onChange Handler', () => {
    it('should call onChange when input changes', async () => {
      const user = userEvent.setup();
      const handleChange = vi.fn();
      renderWithTheme(<BgtSimpleInputField {...defaultProps} onChange={handleChange} />);

      await user.type(screen.getByRole('textbox'), 'a');

      expect(handleChange).toHaveBeenCalled();
    });

    it('should pass event to onChange handler', async () => {
      const user = userEvent.setup();
      const handleChange = vi.fn();
      renderWithTheme(<BgtSimpleInputField {...defaultProps} onChange={handleChange} />);

      await user.type(screen.getByRole('textbox'), 'a');

      expect(handleChange).toHaveBeenCalledWith(expect.objectContaining({ target: expect.any(Object) }));
    });
  });

  describe('Placeholder', () => {
    it('should display placeholder in uppercase', () => {
      renderWithTheme(<BgtSimpleInputField {...defaultProps} placeholder="enter text" />);
      const input = screen.getByRole('textbox');
      expect(input).toHaveAttribute('placeholder', 'ENTER TEXT');
    });

    it('should handle empty placeholder', () => {
      renderWithTheme(<BgtSimpleInputField {...defaultProps} placeholder="" />);
      const input = screen.getByRole('textbox');
      expect(input).toHaveAttribute('placeholder', '');
    });
  });

  describe('Prefix Label', () => {
    it('should render string prefix label', () => {
      renderWithTheme(<BgtSimpleInputField {...defaultProps} prefixLabel="$" />);
      expect(screen.getByText('$')).toBeInTheDocument();
    });

    it('should render ReactNode prefix label', () => {
      renderWithTheme(
        <BgtSimpleInputField {...defaultProps} prefixLabel={<span data-testid="prefix-icon">Icon</span>} />
      );
      expect(screen.getByTestId('prefix-icon')).toBeInTheDocument();
    });
  });

  describe('Suffix Label', () => {
    it('should render string suffix label', () => {
      renderWithTheme(<BgtSimpleInputField {...defaultProps} suffixLabel="kg" />);
      expect(screen.getByText('kg')).toBeInTheDocument();
    });

    it('should render ReactNode suffix label', () => {
      renderWithTheme(
        <BgtSimpleInputField {...defaultProps} suffixLabel={<span data-testid="suffix-icon">Icon</span>} />
      );
      expect(screen.getByTestId('suffix-icon')).toBeInTheDocument();
    });
  });

  describe('Disabled State', () => {
    it('should not be disabled by default', () => {
      renderWithTheme(<BgtSimpleInputField {...defaultProps} />);
      const input = screen.getByRole('textbox');
      expect(input).not.toBeDisabled();
    });

    it('should be disabled when disabled prop is true', () => {
      renderWithTheme(<BgtSimpleInputField {...defaultProps} disabled={true} />);
      const input = screen.getByRole('textbox');
      expect(input).toBeDisabled();
    });
  });

  describe('Combined Props', () => {
    it('should handle multiple props together', () => {
      renderWithTheme(
        <BgtSimpleInputField
          type="text"
          value="test"
          label="Field Label"
          placeholder="enter value"
          prefixLabel="$"
          suffixLabel="USD"
          className="custom-class"
          onChange={vi.fn()}
        />
      );

      expect(screen.getByText('Field Label')).toBeInTheDocument();
      expect(screen.getByText('$')).toBeInTheDocument();
      expect(screen.getByText('USD')).toBeInTheDocument();
      expect(screen.getByRole('textbox')).toHaveValue('test');
      expect(screen.getByRole('textbox')).toHaveAttribute('placeholder', 'ENTER VALUE');
    });
  });

  describe('Edge Cases', () => {
    it('should handle zero as number value', () => {
      renderWithTheme(<BgtSimpleInputField {...defaultProps} type="number" value={0} />);
      const input = screen.getByRole('spinbutton');
      expect(input).toHaveValue(0);
    });

    it('should handle empty string value', () => {
      renderWithTheme(<BgtSimpleInputField {...defaultProps} value="" />);
      const input = screen.getByRole('textbox');
      expect(input).toHaveValue('');
    });
  });
});
