import { describe, it, expect } from 'vitest';

import { BgtText } from './BgtText';

import { screen, renderWithTheme } from '@/test/test-utils';


describe('BgtText', () => {
  describe('Rendering', () => {
    it('should render children text content', () => {
      renderWithTheme(<BgtText>Hello World</BgtText>);
      expect(screen.getByText('Hello World')).toBeInTheDocument();
    });

    it('should render multiple children', () => {
      renderWithTheme(
        <BgtText>
          <span>Part 1</span>
          <span>Part 2</span>
        </BgtText>
      );
      expect(screen.getByText('Part 1')).toBeInTheDocument();
      expect(screen.getByText('Part 2')).toBeInTheDocument();
    });
  });

  describe('Size Prop', () => {
    it('should render with default size', () => {
      renderWithTheme(<BgtText>Default Size</BgtText>);
      expect(screen.getByText('Default Size')).toBeInTheDocument();
    });

    it('should render with size 1', () => {
      renderWithTheme(<BgtText size="1">Size 1</BgtText>);
      expect(screen.getByText('Size 1')).toBeInTheDocument();
    });

    it('should render with size 5', () => {
      renderWithTheme(<BgtText size="5">Size 5</BgtText>);
      expect(screen.getByText('Size 5')).toBeInTheDocument();
    });

    it('should render with size 9', () => {
      renderWithTheme(<BgtText size="9">Size 9</BgtText>);
      expect(screen.getByText('Size 9')).toBeInTheDocument();
    });
  });

  describe('Weight Prop', () => {
    it('should render with bold weight', () => {
      renderWithTheme(<BgtText weight="bold">Bold Text</BgtText>);
      expect(screen.getByText('Bold Text')).toBeInTheDocument();
    });

    it('should render with light weight', () => {
      renderWithTheme(<BgtText weight="light">Light Text</BgtText>);
      expect(screen.getByText('Light Text')).toBeInTheDocument();
    });

    it('should render with regular weight', () => {
      renderWithTheme(<BgtText weight="regular">Regular Text</BgtText>);
      expect(screen.getByText('Regular Text')).toBeInTheDocument();
    });

    it('should render with medium weight', () => {
      renderWithTheme(<BgtText weight="medium">Medium Text</BgtText>);
      expect(screen.getByText('Medium Text')).toBeInTheDocument();
    });
  });

  describe('Color Prop', () => {
    it('should render with default white color', () => {
      renderWithTheme(<BgtText>White Text</BgtText>);
      expect(screen.getByText('White Text')).toBeInTheDocument();
    });

    it('should render with amber color', () => {
      renderWithTheme(<BgtText color="amber">Amber Text</BgtText>);
      expect(screen.getByText('Amber Text')).toBeInTheDocument();
    });

    it('should render with primary color', () => {
      renderWithTheme(<BgtText color="primary">Primary Text</BgtText>);
      expect(screen.getByText('Primary Text')).toBeInTheDocument();
    });
  });

  describe('Opacity Prop', () => {
    it('should render with default opacity', () => {
      renderWithTheme(<BgtText>Full Opacity</BgtText>);
      expect(screen.getByText('Full Opacity')).toBeInTheDocument();
    });

    it('should render with 50% opacity', () => {
      renderWithTheme(<BgtText opacity={50}>50% Opacity</BgtText>);
      expect(screen.getByText('50% Opacity')).toBeInTheDocument();
    });
  });

  describe('ClassName Prop', () => {
    it('should apply custom className', () => {
      renderWithTheme(<BgtText className="custom-class">Text</BgtText>);
      const element = screen.getByText('Text');
      expect(element).toHaveClass('custom-class');
    });

    it('should accept multiple custom classes', () => {
      renderWithTheme(<BgtText className="class-one class-two">Text</BgtText>);
      const element = screen.getByText('Text');
      expect(element).toHaveClass('class-one');
      expect(element).toHaveClass('class-two');
    });
  });

  describe('Additional Props', () => {
    it('should pass through data attributes', () => {
      renderWithTheme(<BgtText data-testid="custom-text">Text</BgtText>);
      expect(screen.getByTestId('custom-text')).toBeInTheDocument();
    });

    it('should pass through id attribute', () => {
      renderWithTheme(<BgtText id="text-id">Text</BgtText>);
      const element = screen.getByText('Text');
      expect(element).toHaveAttribute('id', 'text-id');
    });

    it('should support aria attributes', () => {
      renderWithTheme(<BgtText aria-label="Accessible Text">Text</BgtText>);
      const element = screen.getByText('Text');
      expect(element).toHaveAttribute('aria-label', 'Accessible Text');
    });
  });

  describe('Combined Props', () => {
    it('should handle multiple props together', () => {
      renderWithTheme(
        <BgtText size="5" weight="bold" color="green" opacity={80} className="extra-class">
          Styled Text
        </BgtText>
      );
      const element = screen.getByText('Styled Text');
      expect(element).toHaveClass('extra-class');
    });
  });

  describe('Edge Cases', () => {
    it('should handle empty children', () => {
      renderWithTheme(<BgtText data-testid="empty">{''}</BgtText>);
      expect(screen.getByTestId('empty')).toBeInTheDocument();
    });

    it('should handle numeric children', () => {
      renderWithTheme(<BgtText>{42}</BgtText>);
      expect(screen.getByText('42')).toBeInTheDocument();
    });
  });
});
