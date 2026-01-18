import { describe, it, expect } from 'vitest';
import { screen, renderWithTheme } from '@/test/test-utils';

import { BgtHeading } from './BgtHeading';

describe('BgtHeading', () => {
  describe('Rendering', () => {
    it('should render children text content', () => {
      renderWithTheme(<BgtHeading>Test Heading</BgtHeading>);
      const headings = screen.getAllByRole('heading', { level: 3 });
      expect(headings[headings.length - 1]).toHaveTextContent('Test Heading');
    });

    it('should render as h3 element by default', () => {
      renderWithTheme(<BgtHeading>Heading Text</BgtHeading>);
      const headings = screen.getAllByRole('heading', { level: 3 });
      const heading = headings[headings.length - 1];
      expect(heading.tagName).toBe('H3');
    });

    it('should render multiple children', () => {
      renderWithTheme(
        <BgtHeading>
          <span>Part 1</span>
          <span>Part 2</span>
        </BgtHeading>
      );
      const headings = screen.getAllByRole('heading', { level: 3 });
      const heading = headings[headings.length - 1];
      expect(heading).toBeInTheDocument();
      expect(heading).toHaveTextContent('Part 1Part 2');
    });
  });

  describe('Size Prop', () => {
    it('should render with default size', () => {
      renderWithTheme(<BgtHeading>Default Size</BgtHeading>);
      const headings = screen.getAllByRole('heading', { level: 3 });
      expect(headings[headings.length - 1]).toBeInTheDocument();
    });

    it('should render with size 1', () => {
      renderWithTheme(<BgtHeading size="1">Size 1</BgtHeading>);
      const headings = screen.getAllByRole('heading', { level: 3 });
      expect(headings[headings.length - 1]).toBeInTheDocument();
    });

    it('should render with size 5', () => {
      renderWithTheme(<BgtHeading size="5">Size 5</BgtHeading>);
      const headings = screen.getAllByRole('heading', { level: 3 });
      expect(headings[headings.length - 1]).toBeInTheDocument();
    });

    it('should render with size 9', () => {
      renderWithTheme(<BgtHeading size="9">Size 9</BgtHeading>);
      const headings = screen.getAllByRole('heading', { level: 3 });
      expect(headings[headings.length - 1]).toBeInTheDocument();
    });
  });

  describe('ClassName Prop', () => {
    it('should apply custom className', () => {
      renderWithTheme(<BgtHeading className="custom-class">Heading</BgtHeading>);
      const headings = screen.getAllByRole('heading', { level: 3 });
      const heading = headings[headings.length - 1];
      expect(heading).toHaveClass('custom-class');
    });

    it('should accept multiple custom classes', () => {
      renderWithTheme(<BgtHeading className="class-one class-two">Heading</BgtHeading>);
      const headings = screen.getAllByRole('heading', { level: 3 });
      const heading = headings[headings.length - 1];
      expect(heading).toHaveClass('class-one');
      expect(heading).toHaveClass('class-two');
    });
  });

  describe('Additional Props', () => {
    it('should pass through additional div props', () => {
      renderWithTheme(
        <BgtHeading data-testid="custom-heading" id="heading-id">
          Heading
        </BgtHeading>
      );
      const heading = screen.getByTestId('custom-heading');
      expect(heading).toBeInTheDocument();
      expect(heading).toHaveAttribute('id', 'heading-id');
    });

    it('should support aria attributes', () => {
      renderWithTheme(<BgtHeading aria-label="Custom Label">Heading</BgtHeading>);
      const headings = screen.getAllByRole('heading', { level: 3 });
      const heading = headings[headings.length - 1];
      expect(heading).toHaveAttribute('aria-label', 'Custom Label');
    });

    it('should support data attributes', () => {
      renderWithTheme(<BgtHeading data-custom="value">Heading</BgtHeading>);
      const headings = screen.getAllByRole('heading', { level: 3 });
      const heading = headings[headings.length - 1];
      expect(heading).toHaveAttribute('data-custom', 'value');
    });
  });

  describe('Edge Cases', () => {
    it('should handle empty children gracefully', () => {
      renderWithTheme(<BgtHeading>{''}</BgtHeading>);
      const headings = screen.getAllByRole('heading', { level: 3 });
      const heading = headings[headings.length - 1];
      expect(heading).toBeInTheDocument();
      expect(heading).toBeEmptyDOMElement();
    });

    it('should handle null/undefined children', () => {
      renderWithTheme(<BgtHeading>{undefined}</BgtHeading>);
      const headings = screen.getAllByRole('heading', { level: 3 });
      const heading = headings[headings.length - 1];
      expect(heading).toBeInTheDocument();
    });

    it('should handle numeric children', () => {
      renderWithTheme(<BgtHeading>{42}</BgtHeading>);
      const headings = screen.getAllByRole('heading', { level: 3 });
      expect(headings[headings.length - 1]).toHaveTextContent('42');
    });

    it('should handle long text content', () => {
      const longText = 'This is a very long heading text that should be truncated due to line-clamp-1 class';
      renderWithTheme(<BgtHeading>{longText}</BgtHeading>);
      const headings = screen.getAllByRole('heading', { level: 3 });
      const heading = headings[headings.length - 1];
      expect(heading).toHaveTextContent(longText);
    });

    it('should handle special characters', () => {
      renderWithTheme(<BgtHeading>Heading with &amp; special &lt;chars&gt;</BgtHeading>);
      const headings = screen.getAllByRole('heading', { level: 3 });
      const heading = headings[headings.length - 1];
      expect(heading).toBeInTheDocument();
    });
  });
});
