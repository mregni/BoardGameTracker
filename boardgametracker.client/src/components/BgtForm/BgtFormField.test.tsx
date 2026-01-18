import { describe, it, expect, vi } from 'vitest';
import { screen, waitFor, userEvent, renderWithTheme } from '@/test/test-utils';
import { useForm } from '@tanstack/react-form';
import { z } from 'zod';

import { BgtFormField } from './BgtFormField';

// i18next is mocked globally in setup.ts

const TestSchema = z.object({
  name: z.string().min(2, { message: 'validation.name.min' }),
  email: z.string().email({ message: 'validation.email.invalid' }),
});

interface TestFormData {
  name: string;
  email: string;
}

const TestFormComponent = ({ onFieldValue }: { onFieldValue?: (value: string) => void }) => {
  const form = useForm<TestFormData>({
    defaultValues: {
      name: '',
      email: '',
    },
  });

  return (
    <form>
      <BgtFormField<TestFormData> form={form} name="name" schema={TestSchema.shape.name}>
        {(field) => (
          <div>
            <input
              data-testid="name-input"
              value={field.state.value}
              onChange={(e) => {
                field.handleChange(e.target.value);
                onFieldValue?.(e.target.value);
              }}
              onBlur={field.handleBlur}
            />
            {field.state.meta.errors.length > 0 && <span data-testid="name-error">{field.state.meta.errors[0]}</span>}
          </div>
        )}
      </BgtFormField>
    </form>
  );
};

describe('BgtFormField', () => {
  describe('Rendering', () => {
    it('should render children with field api', () => {
      renderWithTheme(<TestFormComponent />);
      expect(screen.getByTestId('name-input')).toBeInTheDocument();
    });

    it('should start with empty value', () => {
      renderWithTheme(<TestFormComponent />);
      expect(screen.getByTestId('name-input')).toHaveValue('');
    });
  });

  describe('Value Handling', () => {
    it('should update value when typing', async () => {
      const user = userEvent.setup();
      renderWithTheme(<TestFormComponent />);

      const input = screen.getByTestId('name-input');
      await user.type(input, 'John');

      expect(input).toHaveValue('John');
    });

    it('should call onFieldValue callback with new value', async () => {
      const user = userEvent.setup();
      const onFieldValue = vi.fn();
      renderWithTheme(<TestFormComponent onFieldValue={onFieldValue} />);

      const input = screen.getByTestId('name-input');
      await user.type(input, 'J');

      expect(onFieldValue).toHaveBeenCalledWith('J');
    });
  });

  describe('Validation', () => {
    it('should show validation error when value is too short', async () => {
      const user = userEvent.setup();
      renderWithTheme(<TestFormComponent />);

      const input = screen.getByTestId('name-input');
      await user.type(input, 'A');

      await waitFor(() => {
        expect(screen.getByTestId('name-error')).toHaveTextContent('validation.name.min');
      });
    });

    it('should not show error when value is valid', async () => {
      const user = userEvent.setup();
      renderWithTheme(<TestFormComponent />);

      const input = screen.getByTestId('name-input');
      await user.type(input, 'John');

      await waitFor(() => {
        expect(screen.queryByTestId('name-error')).not.toBeInTheDocument();
      });
    });

    it('should validate on change', async () => {
      const user = userEvent.setup();
      renderWithTheme(<TestFormComponent />);

      const input = screen.getByTestId('name-input');
      await user.type(input, 'A');

      await waitFor(() => {
        expect(screen.getByTestId('name-error')).toBeInTheDocument();
      });

      await user.type(input, 'BC');

      await waitFor(() => {
        expect(screen.queryByTestId('name-error')).not.toBeInTheDocument();
      });
    });
  });
});

// Test with different schema types
const NumberSchema = z.object({
  age: z.coerce.number().min(18, { message: 'validation.age.min' }),
});

interface NumberFormData {
  age: number;
}

const NumberFormComponent = () => {
  const form = useForm<NumberFormData>({
    defaultValues: {
      age: 0,
    },
  });

  return (
    <form>
      <BgtFormField<NumberFormData> form={form} name="age" schema={NumberSchema.shape.age}>
        {(field) => (
          <div>
            <input
              data-testid="age-input"
              type="number"
              value={field.state.value}
              onChange={(e) => field.handleChange(Number(e.target.value))}
              onBlur={field.handleBlur}
            />
            {field.state.meta.errors.length > 0 && <span data-testid="age-error">{field.state.meta.errors[0]}</span>}
          </div>
        )}
      </BgtFormField>
    </form>
  );
};

describe('BgtFormField with number schema', () => {
  it('should validate number field', async () => {
    const user = userEvent.setup();
    renderWithTheme(<NumberFormComponent />);

    const input = screen.getByTestId('age-input');
    await user.clear(input);
    await user.type(input, '15');

    await waitFor(() => {
      expect(screen.getByTestId('age-error')).toBeInTheDocument();
    });
  });

  it('should pass validation for valid number', async () => {
    const user = userEvent.setup();
    renderWithTheme(<NumberFormComponent />);

    const input = screen.getByTestId('age-input');
    await user.clear(input);
    await user.type(input, '25');

    await waitFor(() => {
      expect(screen.queryByTestId('age-error')).not.toBeInTheDocument();
    });
  });
});
