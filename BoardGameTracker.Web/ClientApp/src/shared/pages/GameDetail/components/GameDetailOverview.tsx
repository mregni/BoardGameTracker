import '../styles.css';

import {
  Button, Col, Descriptions, Divider, Dropdown, Image, MenuProps, Rate, Row, Space, Tooltip,
  Typography,
} from 'antd';
import useBreakpoint from 'antd/lib/grid/hooks/useBreakpoint';
import React, {useContext, useState} from 'react';
import {Trans, useTranslation} from 'react-i18next';
import {Link, useNavigate} from 'react-router-dom';

import {DeleteOutlined, EditOutlined, MoreOutlined, PlusOutlined} from '@ant-design/icons';

import {
  GcPageContainer, GcPageContainerContent, GcPageContainerHeader,
} from '../../../components/GcPageContainer';
import {GcStateRibbon} from '../../../components/GcStateRibbon';
import {Game} from '../../../models';
import {roundToDecimals, useModals} from '../../../utils';
import {GamesContext} from '../../Games/context';
import {NewPlayFormDrawer} from '../../Plays';
import {GameDetailContext} from '../context/GameDetailState';

const { Title } = Typography;

type Props = {
  game: Game;
}

const GameHeader = (props: Props) => {
  const { game } = props;
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);

  return (
    <Space direction='vertical' align='start'>
      <Space direction='horizontal' align='start'>
        <Link to={``} onClick={() => setOpen(true)}>
          <Button type="primary" icon={<PlusOutlined />} size='large'>{t('game.new-play')}</Button>
        </Link>
      </Space>
      <Descriptions size='small' column={{ xxl: 7, xl: 5, lg: 4, md: 2, sm: 2, xs: 1 }}>
        <Descriptions.Item label="Playtime">{game.minPlayTime} - {game.maxPlayTime} min</Descriptions.Item>
        <Descriptions.Item label="Player count">{game.minPlayers} - {game.maxPlayers}</Descriptions.Item>
        <Descriptions.Item label="Minimum age">{game.minAge}</Descriptions.Item>
        <Descriptions.Item label="Weight">{game.weight} / 5</Descriptions.Item>
      </Descriptions>
      <Descriptions>
        <Descriptions.Item label="Categories">{game.categories.map(x => x.name).join(', ')}</Descriptions.Item>
      </Descriptions>
      <Tooltip placement="right" title={`${game.rating?.toFixed(1)}/10`}>
        <Rate
          disabled
          style={{ fontSize: 10, paddingTop: 10 }}
          allowHalf
          value={roundToDecimals(game.rating, 1) / 2} />
      </Tooltip>
      <NewPlayFormDrawer open={open} setOpen={setOpen} game={game} />
    </Space>)
}

const GameContent = (props: Props) => {
  const { game } = props;
  const { t } = useTranslation();

  return (
    <>
      {/* <Divider type="horizontal" style={{ margin: '12px 0' }} />
      <Row align="middle">
        <Col>
          <Divider type="vertical" style={{ margin: '0 24px 0 0', height: '3rem' }} />
        </Col>
        <Col flex="auto">
          <Statistic title="Weight" value={game.weight ?? 0} suffix="/ 5" />
        </Col>
        <Col>
          <Divider type="vertical" style={{ margin: '0 24px', height: '3rem' }} />
        </Col>
        <Col flex="auto">
          <Statistic title="Rating" value={game.rating ?? 0} precision={1} suffix="/ 10" />
        </Col>
        <Col >
          <Divider type="vertical" style={{ margin: '0 24px', height: '3rem' }} />
        </Col>
        <Col flex="auto">
          <Statistic title="Minimum age" value={game.minAge ?? 0} />
        </Col>
        <Col>
          <Divider type="vertical" style={{ margin: '0 24px', height: '3rem' }} />
        </Col>
        <Col flex="auto">
          <Statistic title="Playtime" value={`${game.minPlayTime} - ${game.maxPlayTime}`} suffix="min" />
        </Col>
        <Col>
          <Divider type="vertical" style={{ margin: '0 24px', height: '3rem' }} />
        </Col>
        <Col flex="auto">
          <Statistic title="Player count" value={`${game.minPlayers} - ${game.maxPlayers}`} />
        </Col>
        <Col>
          <Divider type="vertical" style={{ margin: '0 0 0 24px', height: '3rem' }} />
        </Col>
      </Row>
      <Divider type="horizontal" style={{ margin: '12px 0' }} />
 */}

    </>
  )
}



const GameDetailOverview = (props: Props) => {
  const { game } = props;
  const { loading, deleteGame } = useContext(GameDetailContext);
  const { loadGames } = useContext(GamesContext);
  const navigate = useNavigate();
  const { deleteModal } = useModals();
  const { t } = useTranslation();
  const screens = useBreakpoint();

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

  const items: MenuProps['items'] = [
    {
      key: '1',
      icon: <EditOutlined />,
      label: <Link to={''}>{t('common.edit')}</Link>,
    },
    {
      key: '2',
      icon: <DeleteOutlined />,
      label: <Link onClick={showDeleteModal} to={''}>{t('common.delete')}</Link>,
    }
  ];

  return (
    <GcPageContainer>
      <GcPageContainerHeader
        hasBack
        backNavigation='/games'
        isLoading={loading}
        title={game.title}
      >
        {!screens.md &&
          <Dropdown menu={{ items }} placement="bottomRight" arrow={{ pointAtCenter: true }}>
            <MoreOutlined />
          </Dropdown>
        }
        {screens.md &&
          <>
            <Button type='ghost' disabled icon={<EditOutlined />}>{t('common.edit')}</Button>
            <Divider type="vertical" />
            <Button type='ghost' icon={<DeleteOutlined />} onClick={showDeleteModal} >{t('common.delete')}</Button>
          </>
        }
      </GcPageContainerHeader>
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
            <GameHeader game={game} />
          </Col>
          <Col xs={24}>
            <GameContent game={game} />
          </Col>
        </Row>
      </GcPageContainerContent>
    </GcPageContainer>
  )
}

export default GameDetailOverview