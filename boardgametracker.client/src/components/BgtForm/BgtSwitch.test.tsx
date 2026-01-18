import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, userEvent, renderWithTheme } from '@/test/test-utils';

import { BgtSwitch } from './BgtSwitch';

const createMockField = (value: boolean = false) => ({
  state: {
    value,
    meta: {
      errors: [],
    },
  },
  handleChange: vi.fn(),
  handleBlur: vi.fn(),
});

describe('BgtSwitch', () => {
  let mockField: ReturnType<typeof createMockField>;

  beforeEach(() => {
    mockField = createMockField();
  });

  describe('Rendering', () => {
    it('should render switch with label', () => {
      renderWithTheme(<BgtSwitch field={mockField} label="Enable notifications" />);
      expect(screen.getByText('Enable notifications')).toBeInTheDocument();
    });

    it('should render switch element', () => {
      renderWithTheme(<BgtSwitch field={mockField} label="Toggle" />);
      expect(screen.getByRole('switch')).toBeInTheDocument();
    });

    it('should be unchecked by default', () => {
      renderWithTheme(<BgtSwitch field={mockField} label="Toggle" />);
      expect(screen.getByRole('switch')).not.toBeChecked();
    });

    it('should be checked when field value is true', () => {
      mockField = createMockField(true);
      renderWithTheme(<BgtSwitch field={mockField} label="Toggle" />);
      expect(screen.getByRole('switch')).toBeChecked();
    });
  });

  describe('Interaction', () => {
    it('should call handleChange when toggled on', async () => {
      const user = userEvent.setup();
      renderWithTheme(<BgtSwitch field={mockField} label="Toggle" />);

      await user.click(screen.getByRole('switch'));
      expect(mockField.handleChange).toHaveBeenCalledWith(true);
    });

    it('should call handleChange when toggled off', async () => {
      const user = userEvent.setup();
      mockField = createMockField(true);
      renderWithTheme(<BgtSwitch field={mockField} label="Toggle" />);

      await user.click(screen.getByRole('switch'));
      expect(mockField.handleChange).toHaveBeenCalledWith(false);
    });

    it('should be toggleable via label click', async () => {
      const user = userEvent.setup();
      renderWithTheme(<BgtSwitch field={mockField} label="Click me" />);

      await user.click(screen.getByText('Click me'));
      expect(mockField.handleChange).toHaveBeenCalled();
    });
  });

  describe('Disabled State', () => {
    it('should be disabled when disabled prop is true', () => {
      renderWithTheme(<BgtSwitch field={mockField} label="Toggle" disabled />);
      expect(screen.getByRole('switch')).toBeDisabled();
    });

    it('should not be disabled by default', () => {
      renderWithTheme(<BgtSwitch field={mockField} label="Toggle" />);
      expect(screen.getByRole('switch')).not.toBeDisabled();
    });

    it('should not call handleChange when disabled', async () => {
      const user = userEvent.setup();
      renderWithTheme(<BgtSwitch field={mockField} label="Toggle" disabled />);

      await user.click(screen.getByRole('switch'));
      expect(mockField.handleChange).not.toHaveBeenCalled();
    });
  });
});
