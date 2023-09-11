import {Col, Row} from 'antd';
import React, {useContext, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {PlusOutlined} from '@ant-design/icons';

import {GcCard} from '../../components/GcCard';
import {
  GcMenuItem, GcPageContainer, GcPageContainerContent, GcPageContainerDrawers,
  GcPageContainerHeader,
} from '../../components/GcPageContainer';
import {AddNewPlayerDrawer} from './components/AddNewPlayerDrawer';
import {PlayerContext} from './context';

export const Players = () => {
  const { players, loading } = useContext(PlayerContext);
  const { t } = useTranslation();
  const [openNewPlayer, setOpenNewPlayer] = useState(false);

  const items: GcMenuItem[] = [
    {
      buttonType: 'primary',
      icon: <PlusOutlined />,
      onClick: () => setOpenNewPlayer(true),
      content: t('player.new.button')
    }
  ];

  return (
    <GcPageContainer>
      <GcPageContainerHeader
        title={t('common.players')}
        isLoading={loading}
        items={items}
      />
      <GcPageContainerContent isLoading={loading || players.length === 0}>
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
      </GcPageContainerContent>
      <GcPageContainerDrawers>
        <AddNewPlayerDrawer open={openNewPlayer} setOpen={setOpenNewPlayer} />
      </GcPageContainerDrawers>
    </GcPageContainer>
  )
}
