import { describe, it, expect, vi } from 'vitest';

import { SearchInputField } from './SearchInputField';

import { screen, userEvent, renderWithTheme } from '@/test/test-utils';

// i18next is mocked globally in setup.ts

vi.mock('@/assets/icons/magnifying-glass.svg?react', () => ({
  default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="search-icon" {...props} />,
}));

describe('SearchInputField', () => {
  const defaultProps = {
    value: '',
    onChange: vi.fn(),
  };

  describe('Rendering', () => {
    it('should render input element', () => {
      renderWithTheme(<SearchInputField {...defaultProps} />);
      const input = screen.getByRole('textbox');
      expect(input).toBeInTheDocument();
    });

    it('should render search icon', () => {
      renderWithTheme(<SearchInputField {...defaultProps} />);
      expect(screen.getByTestId('search-icon')).toBeInTheDocument();
    });
  });

  describe('Placeholder', () => {
    it('should use translated default placeholder when not provided', () => {
      renderWithTheme(<SearchInputField {...defaultProps} />);
      const input = screen.getByRole('textbox');
      expect(input).toHaveAttribute('placeholder', 'COMMON.FILTER-NAME');
    });

    it('should use custom placeholder when provided', () => {
      renderWithTheme(<SearchInputField {...defaultProps} placeholder="search games" />);
      const input = screen.getByRole('textbox');
      expect(input).toHaveAttribute('placeholder', 'SEARCH GAMES');
    });
  });

  describe('Value Handling', () => {
    it('should display string value', () => {
      renderWithTheme(<SearchInputField {...defaultProps} value="test query" />);
      const input = screen.getByRole('textbox');
      expect(input).toHaveValue('test query');
    });

    it('should display number value', () => {
      renderWithTheme(<SearchInputField {...defaultProps} value={42} />);
      const input = screen.getByRole('textbox');
      expect(input).toHaveValue('42');
    });

    it('should handle undefined value', () => {
      renderWithTheme(<SearchInputField {...defaultProps} value={undefined} />);
      const input = screen.getByRole('textbox');
      expect(input).toHaveValue('');
    });
  });

  describe('onChange Handler', () => {
    it('should call onChange when typing', async () => {
      const user = userEvent.setup();
      const handleChange = vi.fn();
      renderWithTheme(<SearchInputField {...defaultProps} onChange={handleChange} />);

      await user.type(screen.getByRole('textbox'), 'a');

      expect(handleChange).toHaveBeenCalled();
    });
  });

  describe('Combined Props', () => {
    it('should handle all props together', async () => {
      const user = userEvent.setup();
      const handleChange = vi.fn();

      renderWithTheme(
        <SearchInputField
          value="initial"
          onChange={handleChange}
          placeholder="search"
          className="wrapper-class"
          inputClassName="input-class"
        />
      );

      const input = screen.getByRole('textbox');
      expect(input).toHaveValue('initial');
      expect(input).toHaveAttribute('placeholder', 'SEARCH');

      await user.type(input, 'x');
      expect(handleChange).toHaveBeenCalled();
    });
  });
});
