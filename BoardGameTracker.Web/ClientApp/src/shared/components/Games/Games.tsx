import {FloatButton} from 'antd';
import React, {useState} from 'react';
import {useTranslation} from 'react-i18next';

import {GlobalOutlined, PlusOutlined} from '@ant-design/icons';

import {GamesContextProvider} from './context';
import SearchGameDrawer from './SearchGameDrawer';

const GamesContainer = () => {
  const { t } = useTranslation();
  const [openNewBggGame, setOpenNewBggGame] = useState(false);
  return (
    <>
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

export const Games = () => {
  return (
    <GamesContextProvider>
      <GamesContainer />
    </GamesContextProvider>
  )
}
