import {TablePaginationConfig} from 'antd/es/table';

export interface DefaultPagination {
  getPagination: (length: number) => TablePaginationConfig
}

export const usePagination = (): DefaultPagination => {
  const getPagination = (length: number): TablePaginationConfig => {
    return {
      position: ['bottomRight'],
      total: length,
      defaultCurrent: 1,
      hideOnSinglePage: false,
      showSizeChanger: true,
      showTitle: true
    }
  }

  return {getPagination}
}