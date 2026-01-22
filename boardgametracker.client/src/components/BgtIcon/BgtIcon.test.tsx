import { describe, it, expect } from 'vitest';

import { BgtIcon } from './BgtIcon';

import { render, screen } from '@/test/test-utils';

describe('BgtIcon', () => {
  describe('Rendering', () => {
    it('should render the icon node', () => {
      render(<BgtIcon icon={<svg data-testid="test-icon" />} />);
      expect(screen.getByTestId('test-icon')).toBeInTheDocument();
    });

    it('should render text as icon', () => {
      render(<BgtIcon icon={<span>Icon Text</span>} />);
      expect(screen.getByText('Icon Text')).toBeInTheDocument();
    });

    it('should render complex ReactNode as icon', () => {
      render(
        <BgtIcon
          icon={
            <div data-testid="complex-icon">
              <span>Nested</span>
              <span>Content</span>
            </div>
          }
        />
      );
      expect(screen.getByTestId('complex-icon')).toBeInTheDocument();
      expect(screen.getByText('Nested')).toBeInTheDocument();
      expect(screen.getByText('Content')).toBeInTheDocument();
    });

    it('should wrap icon in a div', () => {
      const { container } = render(<BgtIcon icon={<span>Icon</span>} />);
      const wrapper = container.firstChild;
      expect(wrapper?.nodeName).toBe('DIV');
    });
  });

  describe('ClassName Prop', () => {
    it('should apply className to wrapper div', () => {
      const { container } = render(<BgtIcon icon={<span>Icon</span>} className="custom-class" />);
      const wrapper = container.firstChild as HTMLElement;
      expect(wrapper).toHaveClass('custom-class');
    });

    it('should apply multiple classes', () => {
      const { container } = render(<BgtIcon icon={<span>Icon</span>} className="class-one class-two" />);
      const wrapper = container.firstChild as HTMLElement;
      expect(wrapper).toHaveClass('class-one');
      expect(wrapper).toHaveClass('class-two');
    });

    it('should work without className', () => {
      const { container } = render(<BgtIcon icon={<span>Icon</span>} />);
      const wrapper = container.firstChild as HTMLElement;
      expect(wrapper).not.toHaveAttribute('class');
    });
  });

  describe('Edge Cases', () => {
    it('should handle null icon gracefully', () => {
      const { container } = render(<BgtIcon icon={null} />);
      const wrapper = container.firstChild as HTMLElement;
      expect(wrapper).toBeInTheDocument();
      expect(wrapper.childNodes.length).toBe(0);
    });

    it('should handle undefined icon gracefully', () => {
      const { container } = render(<BgtIcon icon={undefined} />);
      const wrapper = container.firstChild as HTMLElement;
      expect(wrapper).toBeInTheDocument();
    });

    it('should handle empty string icon', () => {
      const { container } = render(<BgtIcon icon="" />);
      const wrapper = container.firstChild as HTMLElement;
      expect(wrapper).toBeInTheDocument();
    });

    it('should handle number as icon', () => {
      render(<BgtIcon icon={42} />);
      expect(screen.getByText('42')).toBeInTheDocument();
    });
  });
});
