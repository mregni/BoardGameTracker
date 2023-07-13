import {FloatButton} from 'antd';
import React, {useState} from 'react';
import {useTranslation} from 'react-i18next';
import {Route, Routes} from 'react-router-dom';

import {GlobalOutlined, PlusOutlined} from '@ant-design/icons';

import SearchGameDrawer from './components/SearchGameDrawer';
import GameList from './GameList';

export const Games = () => {
  const { t } = useTranslation();
  const [openNewBggGame, setOpenNewBggGame] = useState(false);
  return (
    <>
      <GameList />
      <FloatButton.Group
        trigger="hover"
        style={{ right: 48 }}
        icon={<PlusOutlined />}
        type="primary"
      >
        <FloatButton tooltip={t('games.manual')} icon={<PlusOutlined />} />
        <FloatButton
          tooltip={t('games.bgg')}
          icon={<GlobalOutlined />}
          onClick={() => setOpenNewBggGame(true)} />
      </FloatButton.Group>
      <SearchGameDrawer setOpen={setOpenNewBggGame} open={openNewBggGame} />
    </>
  )
}

