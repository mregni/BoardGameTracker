import {Button, Drawer, Space} from 'antd';
import useBreakpoint from 'antd/lib/grid/hooks/useBreakpoint';
import {t} from 'i18next';
import React, {ReactNode} from 'react';

interface Props {
  onClose: () => void;
  open: boolean;
  title: string;
  children: ReactNode | ReactNode[]
  extraButtons?: ReactNode | ReactNode[];
}

export const GcDrawer = (props: Props) => {
  const { onClose, open, children, title, extraButtons } = props;
  const screens = useBreakpoint();

  const getDrawerWith = () => {
    return screens.md ? 700 : '100%';
  }

  return (
    <Drawer
      title={title}
      onClose={onClose}
      closable={false}
      width={getDrawerWith()}
      open={open}
      bodyStyle={{ paddingBottom: 80 }}
      extra={
        <Space>
          <Button onClick={onClose}>{t('common.cancel')}</Button>
          {extraButtons}
        </Space>
      }
    >
      <>{children}</>
    </Drawer>
  )
}
