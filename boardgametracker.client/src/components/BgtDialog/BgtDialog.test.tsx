import { describe, it, expect } from 'vitest';
import { screen, renderWithTheme, render } from '@/test/test-utils';

import { BgtDialog, BgtDialogContent, BgtDialogTitle, BgtDialogDescription, BgtDialogClose } from './BgtDialog';

describe('BgtDialog', () => {
  describe('BgtDialog Root', () => {
    it('should render children when open', () => {
      renderWithTheme(
        <BgtDialog open={true}>
          <div data-testid="dialog-child">Dialog Content</div>
        </BgtDialog>
      );
      expect(screen.getByTestId('dialog-child')).toBeInTheDocument();
    });

    it('should render children when closed', () => {
      renderWithTheme(
        <BgtDialog open={false}>
          <div data-testid="dialog-child">Dialog Content</div>
        </BgtDialog>
      );
      expect(screen.getByTestId('dialog-child')).toBeInTheDocument();
    });
  });

  describe('BgtDialogTitle', () => {
    it('should render title text', () => {
      renderWithTheme(
        <BgtDialog open={true}>
          <BgtDialogContent>
            <BgtDialogTitle>My Dialog Title</BgtDialogTitle>
          </BgtDialogContent>
        </BgtDialog>
      );
      expect(screen.getByText('My Dialog Title')).toBeInTheDocument();
    });

    it('should apply custom className', () => {
      renderWithTheme(
        <BgtDialog open={true}>
          <BgtDialogContent>
            <BgtDialogTitle className="custom-title">Title</BgtDialogTitle>
          </BgtDialogContent>
        </BgtDialog>
      );
      const title = screen.getByText('Title');
      expect(title).toHaveClass('custom-title');
    });
  });

  describe('BgtDialogDescription', () => {
    it('should render description text', () => {
      renderWithTheme(
        <BgtDialog open={true}>
          <BgtDialogContent>
            <BgtDialogDescription>This is a description</BgtDialogDescription>
          </BgtDialogContent>
        </BgtDialog>
      );
      expect(screen.getByText('This is a description')).toBeInTheDocument();
    });

    it('should apply custom className', () => {
      renderWithTheme(
        <BgtDialog open={true}>
          <BgtDialogContent>
            <BgtDialogDescription className="custom-desc">Description</BgtDialogDescription>
          </BgtDialogContent>
        </BgtDialog>
      );
      const desc = screen.getByText('Description');
      expect(desc).toHaveClass('custom-desc');
    });
  });

  describe('BgtDialogClose', () => {
    it('should render children', () => {
      render(
        <BgtDialogClose>
          <button>Cancel</button>
          <button>Confirm</button>
        </BgtDialogClose>
      );
      expect(screen.getByText('Cancel')).toBeInTheDocument();
      expect(screen.getByText('Confirm')).toBeInTheDocument();
    });

    it('should apply custom className', () => {
      const { container } = render(
        <BgtDialogClose className="custom-close">
          <button>Cancel</button>
        </BgtDialogClose>
      );
      const wrapper = container.firstChild as HTMLElement;
      expect(wrapper).toHaveClass('custom-close');
    });
  });

  describe('BgtDialogContent', () => {
    it('should render children', () => {
      renderWithTheme(
        <BgtDialog open={true}>
          <BgtDialogContent>
            <div data-testid="content-child">Content</div>
          </BgtDialogContent>
        </BgtDialog>
      );
      expect(screen.getByTestId('content-child')).toBeInTheDocument();
    });

    it('should apply custom className', () => {
      renderWithTheme(
        <BgtDialog open={true}>
          <BgtDialogContent className="custom-content" data-testid="dialog-content">
            Content
          </BgtDialogContent>
        </BgtDialog>
      );
      const content = screen.getByTestId('dialog-content');
      expect(content).toHaveClass('custom-content');
    });
  });

  describe('Combined Usage', () => {
    it('should render full dialog structure', () => {
      renderWithTheme(
        <BgtDialog open={true}>
          <BgtDialogContent>
            <BgtDialogTitle>Confirm Action</BgtDialogTitle>
            <BgtDialogDescription>Are you sure you want to proceed?</BgtDialogDescription>
            <BgtDialogClose>
              <button>Cancel</button>
              <button>Confirm</button>
            </BgtDialogClose>
          </BgtDialogContent>
        </BgtDialog>
      );

      expect(screen.getByText('Confirm Action')).toBeInTheDocument();
      expect(screen.getByText('Are you sure you want to proceed?')).toBeInTheDocument();
      expect(screen.getByText('Cancel')).toBeInTheDocument();
      expect(screen.getByText('Confirm')).toBeInTheDocument();
    });
  });
});
