import {Button, Col, Image, Row, Space, Typography} from 'antd';
import useBreakpoint from 'antd/lib/grid/hooks/useBreakpoint';
import React, {useContext, useEffect, useState} from 'react';
import {Trans, useTranslation} from 'react-i18next';
import {useNavigate} from 'react-router-dom';

import {ArrowLeftOutlined, DeleteOutlined, EditOutlined} from '@ant-design/icons';

import GcBackButton from '../../../components/GcBackButton/GcBackButton';
import {Player} from '../../../models';
import {useModals} from '../../../utils';
import {PlayerContext} from '../../Players/context';
import {NewPlayFormDrawer} from '../../Plays';
import {PlayerDetailContext} from '../context/PlayerDetailState';

const { Title } = Typography;

interface Props {
  player: Player;
}


const PlayerHeader = (props: Props) => {
  const { deletePlayer, loading } = useContext(PlayerDetailContext);
  const { loadPlayers } = useContext(PlayerContext);
  const { t } = useTranslation();
  const navigate = useNavigate();
  const {deleteModal} = useModals();
  const { player } = props;

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
    await deletePlayer();
    await loadPlayers();
    navigate('/players');
  }

  return (
    <div style={{ position: 'relative' }}>
      <GcBackButton onClick={() => navigate('/players')} disabled={loading} />
      <Space direction='vertical' align='start'>
        <Row gutter={8}>
          <Col flex="auto">
            <Title level={2} style={{ margin: 0 }}>
              {player.name}
            </Title>
          </Col>
        </Row>
        <Space direction='horizontal' align='start'>
          <Button size='small' type='primary' disabled icon={<EditOutlined />}>{t('common.edit')}</Button>
          <Button size='small' type='primary' icon={<DeleteOutlined />} danger onClick={() => showDeleteModal()}>{t('common.delete')}</Button>
        </Space>
      </Space>
    </div>
  )
}

export const PlayerDetailOverview = (props: Props) => {
  const { player } = props;
  return (
    <Row gutter={[16, 16]}>
      <Col xxl={3} xl={4} md={5} xs={24}>
        <Image
          preview={false}
          width={'100%'}
          src={`https://localhost:7178/${player.image}`}
          className="image"
        />
      </Col>
      <Col xxl={21} xl={20} md={19} xs={24}>
        <PlayerHeader player={player} />
      </Col>
    </Row>
  )
}
