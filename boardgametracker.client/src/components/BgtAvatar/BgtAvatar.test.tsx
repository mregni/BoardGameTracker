import { describe, it, expect, vi } from 'vitest';

import { BgtAvatar } from './BgtAvatar';

import { render, screen, renderWithTheme, userEvent } from '@/test/test-utils';

describe('BgtAvatar', () => {
  describe('Rendering', () => {
    it('should render image when provided', () => {
      renderWithTheme(<BgtAvatar title="John" image="/avatar.png" />);
      const img = screen.getByRole('img');
      expect(img).toBeInTheDocument();
      expect(img).toHaveAttribute('src', '/avatar.png');
    });

    it('should render first letter of title when no image', () => {
      renderWithTheme(<BgtAvatar title="John" image={null} />);
      expect(screen.getByText('J')).toBeInTheDocument();
    });

    it('should use alt attribute from title', () => {
      renderWithTheme(<BgtAvatar title="John Doe" image="/avatar.png" />);
      const img = screen.getByRole('img');
      expect(img).toHaveAttribute('alt', 'John Doe');
    });

    it('should return null when no image and no title', () => {
      const { container } = render(<BgtAvatar image={null} />);
      expect(container.firstChild).toBeNull();
    });

    it('should return null when image is undefined and no title', () => {
      const { container } = render(<BgtAvatar image={undefined} />);
      expect(container.firstChild).toBeNull();
    });

    it('should render title initial when image is undefined but title exists', () => {
      renderWithTheme(<BgtAvatar title="Alice" image={undefined} />);
      expect(screen.getByText('A')).toBeInTheDocument();
    });
  });

  describe('Size Variants', () => {
    it('should render with small size', () => {
      renderWithTheme(<BgtAvatar title="A" image="/avatar.png" size="small" />);
      expect(screen.getByRole('img')).toBeInTheDocument();
    });

    it('should render with medium size by default', () => {
      renderWithTheme(<BgtAvatar title="A" image="/avatar.png" />);
      expect(screen.getByRole('img')).toBeInTheDocument();
    });

    it('should render with large size', () => {
      renderWithTheme(<BgtAvatar title="A" image="/avatar.png" size="large" />);
      expect(screen.getByRole('img')).toBeInTheDocument();
    });

    it('should render with big size', () => {
      renderWithTheme(<BgtAvatar title="A" image="/avatar.png" size="big" />);
      expect(screen.getByRole('img')).toBeInTheDocument();
    });
  });

  describe('Text Size Mapping', () => {
    it('should render initial for small avatar', () => {
      renderWithTheme(<BgtAvatar title="John" image={null} size="small" />);
      expect(screen.getByText('J')).toBeInTheDocument();
    });

    it('should render initial for big avatar', () => {
      renderWithTheme(<BgtAvatar title="John" image={null} size="big" />);
      expect(screen.getByText('J')).toBeInTheDocument();
    });
  });

  describe('Color Prop', () => {
    it('should apply background color to initial avatar', () => {
      renderWithTheme(<BgtAvatar title="John" image={null} color="hsl(200, 85%, 35%)" />);
      const avatar = screen.getByText('J').parentElement;
      expect(avatar).toHaveStyle({ backgroundColor: 'hsl(200, 85%, 35%)' });
    });
  });

  describe('Interactive State', () => {
    it('should call onClick when clicked (image)', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      renderWithTheme(<BgtAvatar title="A" image="/avatar.png" onClick={handleClick} />);

      await user.click(screen.getByRole('img'));

      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('should call onClick when clicked (initial)', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      renderWithTheme(<BgtAvatar title="John" image={null} onClick={handleClick} />);

      await user.click(screen.getByText('J'));

      expect(handleClick).toHaveBeenCalledTimes(1);
    });
  });

  describe('Combined Props', () => {
    it('should handle multiple props together', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();

      renderWithTheme(
        <BgtAvatar title="Alice" image={null} size="large" color="hsl(100, 85%, 35%)" onClick={handleClick} />
      );

      const avatar = screen.getByText('A').parentElement;
      expect(avatar).toHaveStyle({ backgroundColor: 'hsl(100, 85%, 35%)' });

      await user.click(screen.getByText('A'));
      expect(handleClick).toHaveBeenCalled();
    });
  });

  describe('Edge Cases', () => {
    it('should handle empty string title', () => {
      const { container } = render(<BgtAvatar title="" image={null} />);
      expect(container.firstChild).toBeNull();
    });

    it('should handle single character title', () => {
      renderWithTheme(<BgtAvatar title="A" image={null} />);
      expect(screen.getByText('A')).toBeInTheDocument();
    });

    it('should handle title with spaces (use first character)', () => {
      renderWithTheme(<BgtAvatar title="John Doe" image={null} />);
      expect(screen.getByText('J')).toBeInTheDocument();
    });
  });
});
