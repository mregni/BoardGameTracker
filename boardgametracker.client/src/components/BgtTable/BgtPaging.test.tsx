import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, userEvent, renderWithTheme, render } from '@/test/test-utils';

import { BgtPaging } from './BgtPaging';

// i18next is mocked globally in setup.ts

describe('BgtPaging', () => {
  const defaultProps = {
    page: 0,
    setPage: vi.fn(),
    totalCount: 50,
    countPerPage: 10,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Rendering', () => {
    it('should render pagination when totalCount exceeds countPerPage', () => {
      renderWithTheme(<BgtPaging {...defaultProps} />);
      expect(screen.getByText('common.previous-page')).toBeInTheDocument();
      expect(screen.getByText('common.next-page')).toBeInTheDocument();
    });

    it('should return null when totalCount is less than countPerPage', () => {
      const { container } = render(
        <BgtPaging {...defaultProps} totalCount={5} countPerPage={10} />
      );
      expect(container.firstChild).toBeNull();
    });

    it('should return null when totalCount equals countPerPage', () => {
      const { container } = render(
        <BgtPaging {...defaultProps} totalCount={10} countPerPage={10} />
      );
      expect(container.firstChild).toBeNull();
    });

    it('should display current page and total pages', () => {
      renderWithTheme(<BgtPaging {...defaultProps} page={2} totalCount={50} countPerPage={10} />);
      expect(screen.getByText('3 / 5')).toBeInTheDocument();
    });
  });

  describe('Navigation Buttons', () => {
    it('should disable Previous button on first page', () => {
      renderWithTheme(<BgtPaging {...defaultProps} page={0} />);
      expect(screen.getByText('common.previous-page')).toBeDisabled();
    });

    it('should enable Previous button on non-first page', () => {
      renderWithTheme(<BgtPaging {...defaultProps} page={2} />);
      expect(screen.getByText('common.previous-page')).not.toBeDisabled();
    });

    it('should disable Next button on last page', () => {
      renderWithTheme(<BgtPaging {...defaultProps} page={4} totalCount={50} countPerPage={10} />);
      expect(screen.getByText('common.next-page')).toBeDisabled();
    });

    it('should enable Next button when not on last page', () => {
      renderWithTheme(<BgtPaging {...defaultProps} page={0} />);
      expect(screen.getByText('common.next-page')).not.toBeDisabled();
    });
  });

  describe('Click Handlers', () => {
    it('should call setPage with decrement function when Previous is clicked', async () => {
      const user = userEvent.setup();
      const setPage = vi.fn();
      renderWithTheme(<BgtPaging {...defaultProps} page={2} setPage={setPage} />);

      await user.click(screen.getByText('common.previous-page'));

      expect(setPage).toHaveBeenCalledTimes(1);
      // Test the function passed to setPage
      const updateFn = setPage.mock.calls[0][0];
      expect(updateFn(2)).toBe(1);
    });

    it('should call setPage with increment function when Next is clicked', async () => {
      const user = userEvent.setup();
      const setPage = vi.fn();
      renderWithTheme(<BgtPaging {...defaultProps} page={1} setPage={setPage} />);

      await user.click(screen.getByText('common.next-page'));

      expect(setPage).toHaveBeenCalledTimes(1);
      // Test the function passed to setPage
      const updateFn = setPage.mock.calls[0][0];
      expect(updateFn(1)).toBe(2);
    });

    it('should not call setPage when Previous is disabled', async () => {
      const user = userEvent.setup();
      const setPage = vi.fn();
      renderWithTheme(<BgtPaging {...defaultProps} page={0} setPage={setPage} />);

      await user.click(screen.getByText('common.previous-page'));

      expect(setPage).not.toHaveBeenCalled();
    });

    it('should not call setPage when Next is disabled', async () => {
      const user = userEvent.setup();
      const setPage = vi.fn();
      renderWithTheme(<BgtPaging {...defaultProps} page={4} totalCount={50} countPerPage={10} setPage={setPage} />);

      await user.click(screen.getByText('common.next-page'));

      expect(setPage).not.toHaveBeenCalled();
    });
  });

  describe('Page Calculations', () => {
    it('should calculate total pages correctly', () => {
      renderWithTheme(<BgtPaging {...defaultProps} totalCount={25} countPerPage={10} />);
      expect(screen.getByText('1 / 3')).toBeInTheDocument();
    });

    it('should handle exact division', () => {
      renderWithTheme(<BgtPaging {...defaultProps} totalCount={30} countPerPage={10} />);
      expect(screen.getByText('1 / 3')).toBeInTheDocument();
    });

    it('should handle partial pages', () => {
      renderWithTheme(<BgtPaging {...defaultProps} totalCount={31} countPerPage={10} />);
      expect(screen.getByText('1 / 4')).toBeInTheDocument();
    });
  });
});
