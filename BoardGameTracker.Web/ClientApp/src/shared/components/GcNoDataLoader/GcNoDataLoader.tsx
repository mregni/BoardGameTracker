import {Empty} from 'antd';
import React from 'react';

interface ListLoaderProps {
  isLoading: boolean;
  children: React.ReactNode;
}

export const GcNoDataLoader = (props: ListLoaderProps) => {
  const { children, isLoading } = props;

  if (isLoading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
        <Empty />
      </div>
    )
  }

  return (
    <>{children}</>
  )
}