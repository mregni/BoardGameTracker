import {Button, Col, Dropdown, Image, MenuProps, Row} from 'antd';
import useBreakpoint from 'antd/lib/grid/hooks/useBreakpoint';
import {useContext} from 'react';
import {Trans, useTranslation} from 'react-i18next';
import {Link, useNavigate} from 'react-router-dom';

import {DeleteOutlined, EditOutlined, MoreOutlined} from '@ant-design/icons';

import {
  GcMenuItem, GcPageContainer, GcPageContainerContent, GcPageContainerHeader,
} from '../../../components/GcPageContainer';
import {GcStateRibbon} from '../../../components/GcStateRibbon';
import {useModals} from '../../../utils';
import {GameDetailContext, GamesContext} from '../context';
import {GameHeader} from './GameHeader';
import {GamePlaysTable} from './GamePlaysTable';
import {GameStatistics} from './GameStatistics';

const GameDetailOverview = () => {
  const { loading, deleteGame, game } = useContext(GameDetailContext);
  const { loadGames } = useContext(GamesContext);
  const navigate = useNavigate();
  const { deleteModal } = useModals();
  const { t } = useTranslation();
  const screens = useBreakpoint();

  if (game === null) {
    return (<></>);
  }

  const showDeleteModal = () => {
    deleteModal(
      t('game.delete.title', { title: game.title }),
      <Trans
        i18nKey="game.delete.description"
        values={{ title: game.title }}
        components={{ strong: <strong />, newline: <br /> }} />,
      localDeleteGame
    );
  }

  const localDeleteGame = async () => {
    await deleteGame();
    await loadGames();
    navigate('/games');
  }

  const items: GcMenuItem[] = [
    {
      buttonType: 'primary',
      icon: <EditOutlined />,
      onClick: () => console.log("edit"),
      content: t('common.edit')
    },
    {
      buttonType: 'primary',
      icon: <DeleteOutlined />,
      onClick: () => showDeleteModal(),
      content: t('common.delete')
    }
  ];

  return (
    <GcPageContainer>
      <GcPageContainerHeader
        hasBack
        backNavigation='/games'
        isLoading={loading}
        title={game.title}
        items={items}
      />
      <GcPageContainerContent isLoading={loading}>
        <Row gutter={[16, 16]}>
          <Col xxl={3} xl={4} md={5} xs={24}>
            <GcStateRibbon state={game.state}>
              <Image
                preview={false}
                width={'100%'}
                src={`https://localhost:7178/${game.image}`}
                className="image"
              />
            </GcStateRibbon>
          </Col>
          <Col xxl={21} xl={20} md={19} xs={24}>
            <GameHeader />
          </Col>
          <Col xs={24}>
            <GameStatistics />
          </Col>
          <Col xs={24}>
            <GamePlaysTable />
          </Col>
        </Row>
      </GcPageContainerContent>
    </GcPageContainer>
  )
}

export default GameDetailOverview
