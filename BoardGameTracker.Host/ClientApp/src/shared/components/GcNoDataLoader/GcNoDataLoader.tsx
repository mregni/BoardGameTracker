import {Empty, Spin} from 'antd';
import React from 'react';

interface ListLoaderProps {
  isLoading: boolean;
  hasData: boolean;
  children: React.ReactNode;
}

export const GcNoDataLoader = (props: ListLoaderProps) => {
  const { children, isLoading, hasData } = props;

  if (isLoading) {
    return (
      <div style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        height: '100%',
        padding: 50
      }}>
        <Spin size="large" />
      </div>
    )
  }

  if (!hasData) {
    return (
      <div style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        height: '100%',
        padding: 50
      }}>

        {!isLoading && !hasData && <Empty />}
      </div>
    )
  }

  return (
    <>{children}</>
  )
}