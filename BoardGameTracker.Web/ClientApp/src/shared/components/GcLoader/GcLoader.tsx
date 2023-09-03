import {Result, Space, Spin} from 'antd';
import React from 'react';
import {useTranslation} from 'react-i18next';

import {blue} from '@ant-design/colors';

type Props = {
  loading: boolean;
  failed?: boolean;
  failedMessage?: string;
  children?: React.ReactNode | React.ReactNode[];
}

export const GcLoader = (props: Props) => {
  const { loading, failed = false, failedMessage = "", children = null } = props;
  const { t } = useTranslation();

  if (loading) {
    return (
      <Space direction='vertical' style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', width: '100vw', height: '100vh' }}>
        <Spin size="large" />
        <span style={{ color: blue[6] }}>{t('common.loading')}</span>
      </Space>
    )
  }

  if (!loading && failed) {
    return (
      <Space direction='vertical' style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', width: '100vw', height: '100vh' }}>
        <Result
          status="error"
          title={t(failedMessage)}
        />
      </Space>
    )
  }

  if (!loading && !failed) {
    return (
      <>{children}</>
    )
  }

  return (<></>)
}
