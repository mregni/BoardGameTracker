import '../styles.css';

import {
  Avatar, Button, Card, Col, Descriptions, Divider, Dropdown, Image, MenuProps, Popconfirm, Rate,
  Row, Space, Statistic, Table, Tooltip,
} from 'antd';
import useBreakpoint from 'antd/lib/grid/hooks/useBreakpoint';
import {addMinutes, format, formatDuration, intervalToDuration, minutesToHours} from 'date-fns';
import React, {ReactElement, ReactNode, useContext, useEffect, useState} from 'react';
import {Trans, useTranslation} from 'react-i18next';
import {Link, useNavigate} from 'react-router-dom';

import {DeleteOutlined, EditOutlined, MoreOutlined, PlusOutlined} from '@ant-design/icons';

import GcBooleanIcon from '../../../components/GcBooleanIcon/GcBooleanIcon';
import {
  GcPageContainer, GcPageContainerContent, GcPageContainerHeader,
} from '../../../components/GcPageContainer';
import {GcStateRibbon} from '../../../components/GcStateRibbon';
import {SettingsContext} from '../../../context/settingsContext';
import {Play, PlayPlayer} from '../../../models';
import {roundToDecimals, useModals} from '../../../utils';
import {GamesContext} from '../../Games/context';
import {PlayerContext} from '../../Players/context';
import {EditPlayDrawer, NewPlayDrawer} from '../../Plays';
import {GameDetailContext} from '../context/GameDetailState';

import type { ColumnsType } from 'antd/es/table';
const GameHeader = () => {
  const { game } = useContext(GameDetailContext);
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);

  if (game === null) {
    return (<></>);
  }

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
      <NewPlayDrawer open={open} setOpen={setOpen} game={game} />
    </Space>)
}

const GameContent = () => {
  const { plays, deletePlay } = useContext(GameDetailContext);
  const { settings } = useContext(SettingsContext);
  const { players } = useContext(PlayerContext);
  const [openPlayEdit, setOpenPlayEdit] = useState(false);
  const [playToEdit, setPlayToEdit] = useState<Play | null>(null);
  const { t } = useTranslation();

  const editPlay = (id: number): void => {
    setPlayToEdit(plays.filter(play => play.id === id)[0]);
    setOpenPlayEdit(true);
  }

  const columns: ColumnsType<Play> = [
    {
      title: 'Date',
      key: 'date',
      render: (data: Play) => {
        return format(data.start, settings.dateTimeFormat);
      }
    },
    {
      title: 'Players',
      key: 'players',
      dataIndex: 'players',
      render: (data: PlayPlayer[]) => {
        return (
          <Avatar.Group>
            {
              data.map((player) => {
                const index = players.findIndex(p => p.id === player.playerId);
                if (index !== undefined) {
                  return (
                    <Tooltip title={players[index].name} placement="top" key={player.playerId}>
                      <Avatar style={{ backgroundColor: '#87d068' }} src={`https://localhost:7178/${players[index].image}`} />
                    </Tooltip>
                  )
                }
                return "";
              })
            }
          </Avatar.Group>
        )
      }
    },
    {
      title: 'Time played',
      key: 'length',
      render: (data: Play) => {
        const hours = minutesToHours(data.minutes);
        const minutes = data.minutes - (hours * 60);
        return formatDuration({ hours: minutesToHours(data.minutes), minutes: minutes });
      }
    },
    {
      title: 'Winners',
      key: 'won',
      dataIndex: 'players',
      render: (data: PlayPlayer[]) => {
        return (
          <Avatar.Group>
            {
              data.filter(x => x.won).map((player) => {
                const index = players.findIndex(p => p.id === player.playerId);
                if (index !== undefined) {
                  return (
                    <Tooltip title={players[index].name} placement="top" key={player.playerId}>
                      <Avatar style={{ backgroundColor: '#87d068' }} src={`https://localhost:7178/${players[index].image}`} />
                    </Tooltip>
                  )
                }
                return "";
              })
            }
          </Avatar.Group>
        )
      }
    },
    {
      title: 'Ended',
      key: 'ended',
      dataIndex: 'ended',
      width: 80,
      render: (x: boolean) => <GcBooleanIcon value={x} />
    },
    {
      title: 'Comment',
      key: 'comment',
      dataIndex: 'comment',
    }, {
      title: 'Actions',
      key: 'actions',
      align: 'right',
      width: 70,
      render: (data: Play) =>
        <Space>
          <Tooltip title="Edit" placement="topRight">
            <EditOutlined onClick={() => editPlay(data.id)} />
          </Tooltip>
          <Popconfirm
            placement="left"
            title={t('play.delete.title')}
            description={t('play.delete.description')}
            onConfirm={() => deletePlay(data.id)}
            okText={t('common.yes')}
            cancelText={t('common.no')}
          >
            <DeleteOutlined style={{ color: '#a8071a' }} />
          </Popconfirm>
        </Space >
    }
  ];

  return (
    <Space direction='vertical' style={{ display: 'flex' }}>
      <Table columns={columns} dataSource={plays} size="small" rowKey={(play: Play) => play.id} />
      {playToEdit && <EditPlayDrawer open={openPlayEdit} setOpen={setOpenPlayEdit} play={playToEdit as Play} />}
    </Space>
  )
}

interface StatisticCard {
  title: string;
  value: string | number | null;
  precision?: number | undefined;
  suffix?: string | undefined;
}

const GameStatistics = () => {
  const { statistics } = useContext(GameDetailContext);
  const [cards, setCards] = useState<StatisticCard[]>([]);

  useEffect(() => {
    if (statistics !== null) {
      setCards([
        { title: "Play count", value: statistics.playCount },
        { title: "Price per play", value: statistics.pricePerPlay, suffix: "€", precision: 2 },
        { title: "Unique players", value: statistics.uniquePlayerCount },
        {
          title: "Total play time", value: formatDuration(
            intervalToDuration({
              start: new Date(2000, 1, 1, 0, 0, 0),
              end: addMinutes(new Date(2000, 1, 1, 0, 0, 0), statistics.totalPlayedTime)
            }),
            { format: ['days', 'hours', 'minutes'], zero: false }
          )
        },
        { title: 'High score', value: statistics.highScore },
        { title: 'Average score', value: statistics.averageScore },
        { title: "Most wins", value: statistics.mostWinsPlayer?.name ?? null }
      ]);
    }
  }, [statistics])


  if (statistics === null) {
    return (<></>);
  }

  console.log(cards)

  return (
    <Row gutter={[16, 16]}>
      {
        cards.map(card => (
          card.value !== null &&
          <Col xs={24} md={12} xl={8} xxl={6}>
            <Card bordered={false}>
              <Statistic
                title={card.title}
                value={card.value}
                suffix={card.suffix}
                precision={card.precision}
              />
            </Card>
          </Col>
        ))
      }
    </Row>
  )
}

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
        {!screens.lg &&
          <Dropdown menu={{ items }} placement="bottomRight" arrow={{ pointAtCenter: true }}>
            <MoreOutlined />
          </Dropdown>
        }
        {screens.lg &&
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
            <GameHeader />
          </Col>
          <Col xs={24}>
            <GameStatistics />
          </Col>
          <Col xs={24}>
            <GameContent />
          </Col>
        </Row>
      </GcPageContainerContent>
    </GcPageContainer>
  )
}

export default GameDetailOverview
