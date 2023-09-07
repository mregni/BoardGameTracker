import {Button, Col, Dropdown, Image, MenuProps, Row, Space} from 'antd';
import useBreakpoint from 'antd/lib/grid/hooks/useBreakpoint';
import {t} from 'i18next';
import React, {useContext} from 'react';
import {Trans} from 'react-i18next';
import {Link, useNavigate} from 'react-router-dom';

import {DeleteOutlined, EditOutlined, MoreOutlined} from '@ant-design/icons';

import {
  GcPageContainer, GcPageContainerContent, GcPageContainerHeader,
} from '../../../components/GcPageContainer';
import {useModals} from '../../../utils';
import {PlayerContext} from '../context';
import {PlayerDetailContext} from '../context/PlayerDetailState';
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
  const { loadPlayers } = useContext(PlayerContext);
  const screens = useBreakpoint();
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
    await deletePlayer();
    await loadPlayers();
    navigate('/players');
  }

  const items: MenuProps['items'] = [
    {
      key: '1',
      icon: <EditOutlined />,
      label: <Link to={''}>{t('common.edit')}</Link>,
    },
    {
      key: '2',
      icon: <DeleteOutlined />,
      label: <Link to={''} onClick={() => showDeleteModal()}>{t('common.delete')}</Link>,
    }
  ];

  if (player === null) {
    return (<></>);
  }

  return (
    <GcPageContainer>
      <GcPageContainerHeader
        hasBack
        backNavigation='/players'
        isLoading={loading}
        title={player.name}
      >
        {!screens.lg &&
          <Dropdown menu={{ items }} placement="bottomRight" arrow={{ pointAtCenter: true }}>
            <Button icon={<MoreOutlined />} type='ghost'></Button>
          </Dropdown>
        }
        {screens.lg &&
          <>
            <Button disabled icon={<EditOutlined />} type="primary">{t('common.edit')}</Button>
            <Button icon={<DeleteOutlined />} danger onClick={() => showDeleteModal()}>{t('common.delete')}</Button>
          </>
        }
      </GcPageContainerHeader>
      <GcPageContainerContent isLoading={loading}>
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
            <PlayerHeader />
          </Col>
          <Col xs={24}>
            <PlayerStatistics />
          </Col>
          <Col xs={24}>
            <PlayerPlaysTable />
          </Col>
        </Row>
      </GcPageContainerContent>
    </GcPageContainer>
  )
}