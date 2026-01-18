import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, userEvent, renderWithTheme } from '@/test/test-utils';

import { BgtImageSelector } from './BgtImageSelector';

// i18next is mocked globally in setup.ts

vi.mock('@/assets/icons/trash.svg?react', () => ({
  default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="trash-icon" {...props} />,
}));

describe('BgtImageSelector', () => {
  let mockSetImage: ReturnType<typeof vi.fn>;
  let mockCreateObjectURL: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    mockSetImage = vi.fn();
    mockCreateObjectURL = vi.fn(() => 'blob:test-url');
    global.URL.createObjectURL = mockCreateObjectURL;
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Rendering', () => {
    it('should render upload area when no image', () => {
      renderWithTheme(<BgtImageSelector image={undefined} setImage={mockSetImage} />);
      expect(screen.getByText('images.upload')).toBeInTheDocument();
    });

    it('should render file types hint', () => {
      renderWithTheme(<BgtImageSelector image={undefined} setImage={mockSetImage} />);
      expect(screen.getByText('images.types')).toBeInTheDocument();
    });

    it('should render file input', () => {
      renderWithTheme(<BgtImageSelector image={undefined} setImage={mockSetImage} />);
      const input = document.querySelector('input[type="file"]');
      expect(input).toBeInTheDocument();
    });
  });

  describe('New Image', () => {
    it('should show preview when image is provided', () => {
      const testFile = new File(['test'], 'test.png', { type: 'image/png' });
      renderWithTheme(<BgtImageSelector image={testFile} setImage={mockSetImage} />);
      const preview = screen.getByAltText('preview image');
      expect(preview).toBeInTheDocument();
    });

    it('should create object URL for new image', () => {
      const testFile = new File(['test'], 'test.png', { type: 'image/png' });
      renderWithTheme(<BgtImageSelector image={testFile} setImage={mockSetImage} />);
      expect(mockCreateObjectURL).toHaveBeenCalledWith(testFile);
    });

    it('should not show upload area when image is set', () => {
      const testFile = new File(['test'], 'test.png', { type: 'image/png' });
      renderWithTheme(<BgtImageSelector image={testFile} setImage={mockSetImage} />);
      expect(screen.queryByText('images.upload')).not.toBeInTheDocument();
    });

    it('should show trash icon on hover for removal', () => {
      const testFile = new File(['test'], 'test.png', { type: 'image/png' });
      renderWithTheme(<BgtImageSelector image={testFile} setImage={mockSetImage} />);
      expect(screen.getByTestId('trash-icon')).toBeInTheDocument();
    });
  });

  describe('Default Image', () => {
    it('should show default image preview', () => {
      renderWithTheme(
        <BgtImageSelector image={undefined} setImage={mockSetImage} defaultImage="https://example.com/image.jpg" />
      );
      const preview = screen.getByAltText('preview image');
      expect(preview).toHaveAttribute('src', 'https://example.com/image.jpg');
    });

    it('should not show upload area when default image exists', () => {
      renderWithTheme(
        <BgtImageSelector image={undefined} setImage={mockSetImage} defaultImage="https://example.com/image.jpg" />
      );
      expect(screen.queryByText('images.upload')).not.toBeInTheDocument();
    });
  });

  describe('File Selection', () => {
    it('should call setImage when file selected via input', async () => {
      renderWithTheme(<BgtImageSelector image={undefined} setImage={mockSetImage} />);

      const input = document.querySelector('input[type="file"]') as HTMLInputElement;
      const testFile = new File(['test'], 'test.png', { type: 'image/png' });

      await userEvent.upload(input, testFile);
      expect(mockSetImage).toHaveBeenCalledWith(testFile);
    });

    it('should handle file drop', () => {
      renderWithTheme(<BgtImageSelector image={undefined} setImage={mockSetImage} />);

      const dropzone = screen.getByText('images.upload').closest('label');
      const testFile = new File(['test'], 'test.png', { type: 'image/png' });

      const dataTransfer = {
        files: [testFile],
        items: [{ kind: 'file', type: 'image/png', getAsFile: () => testFile }],
        types: ['Files'],
      };

      fireEvent.drop(dropzone!, { dataTransfer });
      expect(mockSetImage).toHaveBeenCalledWith(testFile);
    });
  });

  describe('Image Removal', () => {
    it('should call setImage with undefined when removing new image', async () => {
      const user = userEvent.setup();
      const testFile = new File(['test'], 'test.png', { type: 'image/png' });
      renderWithTheme(<BgtImageSelector image={testFile} setImage={mockSetImage} />);

      const trashButton = screen.getByTestId('trash-icon').closest('button');
      await user.click(trashButton!);
      expect(mockSetImage).toHaveBeenCalledWith(undefined);
    });

    it('should call setImage with null when removing default image', async () => {
      const user = userEvent.setup();
      renderWithTheme(
        <BgtImageSelector image={undefined} setImage={mockSetImage} defaultImage="https://example.com/image.jpg" />
      );

      const trashButton = screen.getByTestId('trash-icon').closest('button');
      await user.click(trashButton!);
      expect(mockSetImage).toHaveBeenCalledWith(null);
    });
  });

  describe('Drag and Drop States', () => {
    it('should prevent default on drag over', () => {
      renderWithTheme(<BgtImageSelector image={undefined} setImage={mockSetImage} />);

      const dropzone = screen.getByText('images.upload').closest('label');
      const event = new Event('dragover', { bubbles: true, cancelable: true });
      const preventDefaultSpy = vi.spyOn(event, 'preventDefault');

      dropzone!.dispatchEvent(event);
      expect(preventDefaultSpy).toHaveBeenCalled();
    });
  });
});
