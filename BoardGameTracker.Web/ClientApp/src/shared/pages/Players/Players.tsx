import {Col, FloatButton, Row} from 'antd';
import React, {useContext, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {PlusOutlined} from '@ant-design/icons';

import {GcCard} from '../../components/GcCard';
import {GcNoDataLoader} from '../../components/GcNoDataLoader';
import {AddNewPlayerDrawer} from './components/AddNewPlayerDrawer';
import {PlayerContext} from './context';

export const Players = () => {
  const { players, loading } = useContext(PlayerContext);
  const { t } = useTranslation();
  const [openNewPlayer, setOpenNewPlayer] = useState(false);


  return (
    <>
      <GcNoDataLoader isLoading={loading || players.length === 0}>
        <Row gutter={[10, 10]}>
          {players.map(player =>
            <Col xxl={2} xl={4} md={6} sm={12} xs={12} key={player.id}>
              <GcCard
                id={player.id}
                title={player.name}
                image={player.image}
                detailPage="players" />
            </Col>)}
        </Row>
      </GcNoDataLoader>
      <FloatButton
        tooltip={t('player.new.title')}
        style={{ right: 48 }}
        icon={<PlusOutlined />}
        type="primary"
        onClick={() => setOpenNewPlayer(true)} />
      <AddNewPlayerDrawer open={openNewPlayer} setOpen={setOpenNewPlayer} />
    </>
  )
}

