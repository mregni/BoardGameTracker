import { describe, it, expect, vi } from 'vitest';

import { BgtPageHeader } from './BgtPageHeader';

import { screen, userEvent, renderWithTheme } from '@/test/test-utils';


// i18next is mocked globally in setup.ts

vi.mock('@/assets/icons/arrow-left.svg?react', () => ({
  default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="arrow-left-icon" {...props} />,
}));

describe('BgtPageHeader', () => {
  describe('Rendering', () => {
    it('should render header text when provided', () => {
      renderWithTheme(<BgtPageHeader header="Page Title" />);
      expect(screen.getByText('Page Title')).toBeInTheDocument();
    });

    it('should not render header when not provided', () => {
      renderWithTheme(<BgtPageHeader />);
      expect(screen.queryByRole('heading')).not.toBeInTheDocument();
    });

    it('should render children', () => {
      renderWithTheme(
        <BgtPageHeader>
          <div data-testid="child-content">Child Content</div>
        </BgtPageHeader>
      );
      expect(screen.getAllByTestId('child-content')).toHaveLength(2); // Desktop and mobile
    });
  });

  describe('Back Navigation', () => {
    it('should render back button with text when both provided', async () => {
      const user = userEvent.setup();
      const handleBack = vi.fn();
      renderWithTheme(<BgtPageHeader backAction={handleBack} backText="Go Back" />);

      expect(screen.getByText('Go Back')).toBeInTheDocument();
      await user.click(screen.getByText('Go Back'));
      expect(handleBack).toHaveBeenCalled();
    });

    it('should render icon button when backAction provided without text', async () => {
      const user = userEvent.setup();
      const handleBack = vi.fn();
      renderWithTheme(<BgtPageHeader backAction={handleBack} />);

      expect(screen.getByTestId('arrow-left-icon')).toBeInTheDocument();
      await user.click(screen.getByRole('button'));
      expect(handleBack).toHaveBeenCalled();
    });

    it('should not render back button when no backAction', () => {
      renderWithTheme(<BgtPageHeader header="Title" />);
      expect(screen.queryByTestId('arrow-left-icon')).not.toBeInTheDocument();
    });
  });

  describe('Actions', () => {
    it('should render action buttons', async () => {
      const user = userEvent.setup();
      const handleAction = vi.fn();
      const actions = [{ content: 'Save', variant: 'primary' as const, onClick: handleAction }];

      renderWithTheme(<BgtPageHeader actions={actions} />);

      expect(screen.getByText('Save')).toBeInTheDocument();
      await user.click(screen.getByText('Save'));
      expect(handleAction).toHaveBeenCalled();
    });

    it('should render multiple action buttons', () => {
      const actions = [
        { content: 'Save', variant: 'primary' as const, onClick: vi.fn() },
        { content: 'Cancel', variant: 'secondary' as const, onClick: vi.fn() },
      ];

      renderWithTheme(<BgtPageHeader actions={actions} />);

      expect(screen.getByText('Save')).toBeInTheDocument();
      expect(screen.getByText('Cancel')).toBeInTheDocument();
    });

    it('should translate string action content', () => {
      const actions = [{ content: 'actions.save', variant: 'primary' as const, onClick: vi.fn() }];

      renderWithTheme(<BgtPageHeader actions={actions} />);

      expect(screen.getByText('actions.save')).toBeInTheDocument();
    });

    it('should render ReactNode action content', () => {
      const actions = [
        {
          content: <span data-testid="custom-action">Custom</span>,
          variant: 'primary' as const,
          onClick: vi.fn(),
        },
      ];

      renderWithTheme(<BgtPageHeader actions={actions} />);

      expect(screen.getByTestId('custom-action')).toBeInTheDocument();
    });
  });

  describe('Combined Props', () => {
    it('should handle all props together', async () => {
      const user = userEvent.setup();
      const handleBack = vi.fn();
      const handleAction = vi.fn();

      renderWithTheme(
        <BgtPageHeader
          header="Dashboard"
          backAction={handleBack}
          backText="Home"
          actions={[{ content: 'Refresh', variant: 'primary' as const, onClick: handleAction }]}
        >
          <div data-testid="filter">Filter</div>
        </BgtPageHeader>
      );

      expect(screen.getByText('Dashboard')).toBeInTheDocument();
      expect(screen.getByText('Home')).toBeInTheDocument();
      expect(screen.getByText('Refresh')).toBeInTheDocument();
      expect(screen.getAllByTestId('filter')).toHaveLength(2);

      await user.click(screen.getByText('Home'));
      expect(handleBack).toHaveBeenCalled();

      await user.click(screen.getByText('Refresh'));
      expect(handleAction).toHaveBeenCalled();
    });
  });
});
