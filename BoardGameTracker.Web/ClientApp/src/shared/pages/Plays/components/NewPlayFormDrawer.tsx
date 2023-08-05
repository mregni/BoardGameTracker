import {Avatar, Badge, Button, Checkbox, DatePicker, Form, Input, List, Select, Space} from 'antd';
import React, {useContext, useEffect, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {
  ClockCircleTwoTone, CrownTwoTone, DeleteOutlined, EditOutlined, MinusCircleOutlined, PlusOutlined,
} from '@ant-design/icons';

import {GcDrawer} from '../../../components/GcDrawer';
import {SettingsContext} from '../../../context/settingsContext';
import {ActivePlayer, Game, Play} from '../../../models';
import {GamesContext, GamesContextProvider} from '../../Games/context';
import {PlayerContext, PlayerContextProvider} from '../../Players/context';
import {PlayContextProvider} from '../context/PlayProvider';
import {PlayContext} from '../context/PlayState';
import PlayerSelectorDrawer from './PlayerSelectorDrawer';

interface FormContent {
  comment: string;
  endend: boolean;
  gameId: number;
  sessions: any[];
}
interface Props {
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  open: boolean;
  game?: Game;
}

export const NewPlayFormDrawerContainer = (props: Props) => {
  const { setOpen, open, game = null } = props;
  const { games, loading: loadingGames } = useContext(GamesContext);
  const { players } = useContext(PlayerContext);
  const { settings } = useContext(SettingsContext);
  const { addPlay } = useContext(PlayContext);
  const { t } = useTranslation();
  const [form] = Form.useForm();
  const [openPlayerSelector, setOpenPLayerSelector] = useState(false);
  const [activePlayers, setActivePlayers] = useState<ActivePlayer[]>([]);
  const [nextId, setNextId] = useState(0);
  const [playerToEdit, setPlayerToEdit] = useState<ActivePlayer | undefined>(undefined);

  useEffect(() => {
    if (playerToEdit !== undefined) {
      setOpenPLayerSelector(true);
    }
  }, [playerToEdit])


  const triggerPlaySave = async (values: FormContent) => {
    const { sessions, ...rest } = values;
    const viewModel: Play = {
      ...rest,
      players: activePlayers,
      sessions: sessions.map(x => ({ start: x[0].$d, end: x[1].$d }))
    }

    addPlay(viewModel);
  }

  const onClose = () => {
    form.resetFields();
    setOpen(false);
  };

  const initialValues = {
    gameId: game?.id ?? null,
    ended: true,
    comment: '',
    players: []
  }

  const formItemLayout = {
    labelCol: { span: 4 },
    wrapperCol: { span: 20 }
  };

  const formItemLayoutWithOutLabel = {
    wrapperCol: { span: 20, offset: 4 }
  };

  const createNewPlayer = () => {
    setNextId((prev) => prev + 1);
    setOpenPLayerSelector(true);
  }

  const closeNewPlayerDrawer = (player: ActivePlayer | null) => {
    if (playerToEdit == undefined && player !== null) {
      setActivePlayers((prev) => [...prev, player]);
    }
    if (playerToEdit !== undefined && player !== null) {
      const updatedItems = activePlayers.map(item => item.uiId === player.uiId ? player : item);
      setActivePlayers(updatedItems);
    }

    setOpenPLayerSelector(false);
    setPlayerToEdit(undefined);
  }

  const removePlayer = (id: number) => {
    setActivePlayers((prev) => prev.filter(x => x.uiId !== id));
  }

  const editPlayer = (id: number) => {
    setPlayerToEdit(activePlayers.find(x => x.uiId === id));
  }

  const { Option } = Select;
  const { RangePicker } = DatePicker;

  const mapValue = (value: string | undefined): string => {
    if (value === undefined || value.length === 0) {
      return '';
    }

    return `${value} / `
  }


  const createDescription = (player: ActivePlayer): string => {
    let result = mapValue(player.color);
    result += mapValue(player.score?.toString());
    result += mapValue(player.team);
    result += mapValue(player.characterName);

    return result.substring(0, result.length - 2);
  }

  return (
    <GcDrawer
      title={t('play.new.title')}
      open={open}
      onClose={onClose}>
      <Form
        form={form}
        {...formItemLayout}
        layout="horizontal"
        autoComplete='off'
        hideRequiredMark
        onFinish={triggerPlaySave}
        initialValues={initialValues}
      >
        <Form.Item
          name="gameId"
          label={t('common.game')}
          valuePropName="value"
          rules={[{ required: true, message: t('play.new.game.required') }]}
        >
          <Select
            placeholder={t('play.new.game.placeholder')}
            loading={loadingGames}
            showSearch
            filterOption={(input, option) => ((option?.children ?? '') as string).toLowerCase().includes(input.toLowerCase())}
          >
            {games.map(x => <Option key={x.id} value={x.id} >{x.title}</Option>)}
          </Select>
        </Form.Item>
        <Form.Item
          name="players"
          label={t('common.players')}
        >
          <Space direction='vertical' style={{ width: '100%' }}>
            <Button
              type="primary"
              onClick={() => createNewPlayer()}
              icon={<PlusOutlined />}
            >
              {t('play.new.button')}
            </Button>
            <List
              itemLayout="horizontal"
              dataSource={activePlayers}
              renderItem={(item: ActivePlayer) => {
                const player = players.find(p => p.id === item.playerId);
                return (
                  <List.Item
                    actions={[
                      <Button type='text' onClick={() => editPlayer(item.uiId)}>edit</Button>,
                      <Button type='text' onClick={() => removePlayer(item.uiId)}>delete</Button>]}
                  >
                    <List.Item.Meta
                      avatar={
                        <Badge
                          count={item.firstPlay ? <ClockCircleTwoTone twoToneColor='green' /> : 0}
                          style={{ marginTop: 30 }}
                        >
                          <Badge count={item.won ? <CrownTwoTone twoToneColor='orange' /> : 0}>
                            <Avatar src={`https://localhost:7178/${player?.image ?? ""}`} />
                          </Badge>
                        </Badge>}
                      title={player?.name}
                      description={createDescription(item)}
                    />
                  </List.Item>
                )
              }}
            />
          </Space>
        </Form.Item>
        <Form.List name="sessions">
          {(fields, { add, remove }, { errors }) => {

            if (fields.length === 0) {
              add();
            }

            return (
              <>
                {fields.map((field, index) => (
                  <Form.Item
                    {...(index === 0 ? formItemLayout : formItemLayoutWithOutLabel)}
                    label={index === 0 ? t('play.new.session.title') : ''}
                    required={false}
                    key={field.key}
                  >
                    <Form.Item
                      {...field}
                      validateTrigger={['onChange', 'onBlur']}
                      rules={[{ required: true, message: "Please input session start and end date." }]}
                      noStyle
                    >
                      <RangePicker
                        style={{ width: 'calc(100% - 30px)' }}
                        placeholder={[t('play.new.session.start-placeholder'), t('play.new.session.end-placeholder')]}
                        format={`${settings.longDateFormat} ${settings.timeFormat}`}
                        showTime
                        minuteStep={5} />
                    </Form.Item>
                    {fields.length > 1 ? (
                      <MinusCircleOutlined
                        style={{ marginLeft: '10px' }}
                        onClick={() => remove(field.name)}
                      />
                    ) : null}
                  </Form.Item>
                ))}
                <Form.Item {...formItemLayoutWithOutLabel}>
                  <Button
                    type="dashed"
                    onClick={() => add()}
                    style={{ width: '60%' }}
                    icon={<PlusOutlined />}
                  >
                    {t('play.new.session.extra')}
                  </Button>
                  <Form.ErrorList errors={errors} />
                </Form.Item>
              </>
            )
          }}
        </Form.List>
        <Form.Item label={t('play.new.ended.title')} name="ended" valuePropName="checked">
          <Checkbox>{t('play.new.ended.description')}</Checkbox>
        </Form.Item>
        <Form.Item label={t('common.comment')} name="comment">
          <Input.TextArea rows={4} />
        </Form.Item>
        <Form.Item wrapperCol={{ offset: 1, span: 22 }}>
          <Button
            type="primary"
            htmlType="submit"
            block
          >
            {t('play.new.save')}
          </Button>
        </Form.Item>
      </Form>
      <PlayerSelectorDrawer open={openPlayerSelector} close={closeNewPlayerDrawer} edit={playerToEdit} nextId={nextId} />
    </GcDrawer>
  )
}

export const NewPlayFormDrawer = (props: Props) => {

  return (
    <GamesContextProvider>
      <PlayerContextProvider>
        <PlayContextProvider>
          <NewPlayFormDrawerContainer {...props} />
        </PlayContextProvider>
      </PlayerContextProvider>
    </GamesContextProvider>
  )
}