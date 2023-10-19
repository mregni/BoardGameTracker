import {Col, Image, Row, Space} from 'antd';
import {t} from 'i18next';
import React, {useContext, useState} from 'react';
import {Trans} from 'react-i18next';
import {useNavigate} from 'react-router-dom';

import {DeleteOutlined, EditOutlined} from '@ant-design/icons';

import {
  GcMenuItem, GcPageContainer, GcPageContent, GcPageDrawer, GcPageHeader,
} from '../../../components/GcPageContainer';
import {useModals} from '../../../utils';
import {PlayerContext} from '../context';
import {PlayerDetailContext} from '../context/PlayerDetailState';
import {EditPlayerDrawer} from './EditPlayerDrawer';
import {PlayerPlaysTable} from './PlayerPlaysTable';
import {PlayerStatistics} from './PlayerStatistics';

const PlayerHeader = () => {
  return (
    <div style={{ position: 'relative' }}>
      <Space direction='vertical' align='start'>

      </Space>
    </div>
  )
}

export const PlayerDetailOverview = () => {
  const { deletePlayer, player, loading } = useContext(PlayerDetailContext);
  const [openEditPlayer, setOpenEditPlayer] = useState(false);
  const { loadPlayers } = useContext(PlayerContext);
  const navigate = useNavigate();
  const { deleteModal } = useModals();

  if (player === null) {
    return (<></>);
  }

  const showDeleteModal = () => {
    deleteModal(
      t('player.delete.title', { title: player.name }),
      <Trans
        i18nKey="player.delete.description"
        values={{ title: player.name }}
        components={{ strong: <strong />, newline: <br /> }} />,
      localDeletePlayer
    );
  }

  const localDeletePlayer = async () => {
    await deletePlayer(player.id, player.name);
    await loadPlayers();
    navigate('/players');
  }

  const items: GcMenuItem[] = [
    {
      buttonType: 'primary',
      icon: <EditOutlined />,
      onClick: () => setOpenEditPlayer(true),
      content: t('common.edit')
    },
    {
      buttonType: 'primary',
      icon: <DeleteOutlined />,
      onClick: () => showDeleteModal(),
      content: t('common.delete')
    }
  ];

  if (player === null) {
    return (<></>);
  }

  return (
    <GcPageContainer>
      <GcPageHeader
        hasBack
        backNavigation='/players'
        isLoading={loading}
        title={player.name}
        items={items}
      />
      <GcPageContent isLoading={loading} hasData={player !== null}>
        <Row gutter={[16, 16]}>
          <Col xxl={3} xl={4} md={5} xs={24}>
            <Image
              preview={false}
              width={'100%'}
              src={`/${player.image}`}
              className="image"
            />
          </Col>
          <Col xxl={21} xl={20} md={19} xs={24}>
            <PlayerHeader />
          </Col>
          <Col xs={24}>
            <PlayerStatistics />
          </Col>
          <Col xs={24}>
            <PlayerPlaysTable />
          </Col>
        </Row>
      </GcPageContent>
      <GcPageDrawer>
        <EditPlayerDrawer
          key={player.id}
          open={openEditPlayer}
          player={player}
          setOpen={setOpenEditPlayer}
        />
      </GcPageDrawer>
    </GcPageContainer>
  )
}
