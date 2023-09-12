import {Button, Drawer, Menu} from 'antd';
import {t} from 'i18next';
import React, {useEffect, useState} from 'react';

import {MenuOutlined} from '@ant-design/icons';

import {getMenuItems} from './MenuItems';

export const GcSmallMenu = () => {
  const [open, setOpen] = useState(false);
  const [openPage, setOpenPage] = useState("0");

  const updateState = () => {
    const result = getMenuItems(t).find(item => location.pathname.startsWith(`/${item.label.props.to}`));
    setOpenPage(result?.key?.toString() ?? "0");
    setOpen(false);
  }

  return (
    <>
      <Button icon={<MenuOutlined />} onClick={() => setOpen(true)} type="ghost" />
      <Drawer
        closable={false}
        width={150}
        open={open}
        placement='left'
        bodyStyle={{padding: 0}}
      >
        <Menu
          selectedKeys={[openPage]}
          onClick={updateState}
          mode="inline"
          style={{height: '100%'}}
          items={getMenuItems(t)} />
      </Drawer>
    </>
  )
}
