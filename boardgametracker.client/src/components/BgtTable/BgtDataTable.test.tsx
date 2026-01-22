import { describe, it, expect } from 'vitest';
import { ColumnDef } from '@tanstack/react-table';

import { BgtDataTable } from './BgtDataTable';

import { screen, renderWithTheme } from '@/test/test-utils';

// i18next is mocked globally in setup.ts

interface TestData {
  id: number;
  name: string;
  value: number;
}

const testColumns: ColumnDef<TestData>[] = [
  {
    accessorKey: 'id',
    header: 'ID',
  },
  {
    accessorKey: 'name',
    header: 'Name',
  },
  {
    accessorKey: 'value',
    header: 'Value',
  },
];

const testData: TestData[] = [
  { id: 1, name: 'Item 1', value: 100 },
  { id: 2, name: 'Item 2', value: 200 },
  { id: 3, name: 'Item 3', value: 300 },
];

describe('BgtDataTable', () => {
  describe('Rendering', () => {
    it('should render table', () => {
      renderWithTheme(<BgtDataTable columns={testColumns} data={testData} noDataMessage="No data available" />);
      expect(screen.getByRole('table')).toBeInTheDocument();
    });

    it('should render headers', () => {
      renderWithTheme(<BgtDataTable columns={testColumns} data={testData} noDataMessage="No data available" />);
      expect(screen.getByText('ID')).toBeInTheDocument();
      expect(screen.getByText('Name')).toBeInTheDocument();
      expect(screen.getByText('Value')).toBeInTheDocument();
    });

    it('should render data rows', () => {
      renderWithTheme(<BgtDataTable columns={testColumns} data={testData} noDataMessage="No data available" />);
      expect(screen.getByText('Item 1')).toBeInTheDocument();
      expect(screen.getByText('Item 2')).toBeInTheDocument();
      expect(screen.getByText('Item 3')).toBeInTheDocument();
    });

    it('should render cell values', () => {
      renderWithTheme(<BgtDataTable columns={testColumns} data={testData} noDataMessage="No data available" />);
      expect(screen.getByText('100')).toBeInTheDocument();
      expect(screen.getByText('200')).toBeInTheDocument();
      expect(screen.getByText('300')).toBeInTheDocument();
    });
  });

  describe('Empty State', () => {
    it('should show no data message when data is empty', () => {
      renderWithTheme(<BgtDataTable columns={testColumns} data={[]} noDataMessage="No items found" />);
      expect(screen.getByText('No items found')).toBeInTheDocument();
    });

    it('should not show data rows when empty', () => {
      renderWithTheme(<BgtDataTable columns={testColumns} data={[]} noDataMessage="No items found" />);
      expect(screen.queryByText('Item 1')).not.toBeInTheDocument();
    });
  });

  describe('Loading State', () => {
    it('should show loading message when isLoading', () => {
      renderWithTheme(<BgtDataTable columns={testColumns} data={[]} noDataMessage="No data" isLoading />);
      expect(screen.getByText('common.loading-data')).toBeInTheDocument();
    });

    it('should not show data when loading', () => {
      renderWithTheme(<BgtDataTable columns={testColumns} data={testData} noDataMessage="No data" isLoading />);
      expect(screen.queryByText('Item 1')).not.toBeInTheDocument();
    });

    it('should not show no data message when loading', () => {
      renderWithTheme(<BgtDataTable columns={testColumns} data={[]} noDataMessage="No items found" isLoading />);
      expect(screen.queryByText('No items found')).not.toBeInTheDocument();
    });
  });

  describe('Headers', () => {
    it('should render headers by default', () => {
      renderWithTheme(<BgtDataTable columns={testColumns} data={testData} noDataMessage="No data" />);
      const headers = screen.getAllByRole('columnheader');
      expect(headers).toHaveLength(3);
    });

    it('should not render headers when noHeaders is true', () => {
      renderWithTheme(<BgtDataTable columns={testColumns} data={testData} noDataMessage="No data" noHeaders />);
      expect(screen.queryByText('ID')).not.toBeInTheDocument();
      expect(screen.queryByText('Name')).not.toBeInTheDocument();
      expect(screen.queryByText('Value')).not.toBeInTheDocument();
    });
  });

  describe('Custom Cell Rendering', () => {
    it('should support custom cell renderers', () => {
      const customColumns: ColumnDef<TestData>[] = [
        {
          accessorKey: 'name',
          header: 'Name',
          cell: ({ row }) => <span data-testid="custom-cell">{row.original.name}</span>,
        },
      ];

      renderWithTheme(<BgtDataTable columns={customColumns} data={testData} noDataMessage="No data" />);
      expect(screen.getAllByTestId('custom-cell')).toHaveLength(3);
    });
  });

  describe('Table Structure', () => {
    it('should have correct number of rows', () => {
      renderWithTheme(<BgtDataTable columns={testColumns} data={testData} noDataMessage="No data" />);
      const rows = screen.getAllByRole('row');
      // 1 header row + 3 data rows
      expect(rows).toHaveLength(4);
    });

    it('should have correct number of cells per row', () => {
      renderWithTheme(<BgtDataTable columns={testColumns} data={testData} noDataMessage="No data" />);
      const cells = screen.getAllByRole('cell');
      // 3 columns * 3 rows = 9 cells
      expect(cells).toHaveLength(9);
    });
  });
});
