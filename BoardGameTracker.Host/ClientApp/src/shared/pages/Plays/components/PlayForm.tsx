import {
  Avatar, Badge, Button, Checkbox, DatePicker, Form, FormInstance, Input, List, Select, Space,
} from 'antd';
import {Dayjs} from 'dayjs';
import {useContext, useEffect, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {ClockCircleTwoTone, CrownTwoTone, PlusOutlined} from '@ant-design/icons';

import {GcSelectWithAdd} from '../../../components/GcSelectWithAdd';
import {SettingsContext} from '../../../context/settingsContext';
import {ActivePlayer, Play, PlayPlayer} from '../../../models';
import {convertToAntdFormat} from '../../../utils';
import {GamesContext} from '../../Games/context';
import {NewLocationDrawer} from '../../Location/components/NewLocationDrawer';
import {LocationContext} from '../../Location/context/LocationState';
import {PlayerContext} from '../../Players/context';
import PlayerSelectorDrawer from './PlayerSelectorDrawer';

export interface FormPlay {
  gameId: number | null;
  id: number;
  ended: boolean;
  comment: string;
  start: Dayjs;
  minutes: number;
  players: PlayPlayer[];
  locationId: number | null;
}

interface FormProps {
  form: FormInstance<FormPlay>;
  submitAction: (values: Play) => void;
  initialValues: FormPlay;
}

export const PlayForm = (props: FormProps) => {
  const { form, submitAction, initialValues } = props;
  const { games, loading: loadingGames } = useContext(GamesContext);
  const { players } = useContext(PlayerContext);
  const { settings } = useContext(SettingsContext);
  const { locations, loading: loadingLocations, loadLocations } = useContext(LocationContext);
  const { t } = useTranslation();

  const [openPlayerSelector, setOpenPLayerSelector] = useState(false);
  const [activePlayers, setActivePlayers] = useState<ActivePlayer[]>([]);
  const [nextId, setNextId] = useState(0);
  const [playerToEdit, setPlayerToEdit] = useState<ActivePlayer | undefined>(undefined);
  const [openNewLocationDrawer, setOpenNewLocationDrawer] = useState(false);

  useEffect(() => {
    if (playerToEdit !== undefined) {
      setOpenPLayerSelector(true);
    }
  }, [playerToEdit]);

  useEffect(() => {
    setActivePlayers(initialValues.players.map((player, i) => ({ ...player, uiId: i })));
  }, [initialValues, form]);

  const triggerSubmit = async (values: FormPlay) => {
    const viewModel: Play = {
      ...values,
      start: values.start.toDate(),
      players: activePlayers,
    }

    if (initialValues.id !== undefined) {
      viewModel.id = initialValues.id;
    }

    submitAction(viewModel);
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

  const createNewPlayer = () => {
    setNextId((prev) => prev + 1);
    setOpenPLayerSelector(true);
  }

  const removePlayer = (id: number) => {
    setActivePlayers((prev) => prev.filter(x => x.uiId !== id));
  }

  const editPlayer = (id: number) => {
    setPlayerToEdit(activePlayers.find(x => x.uiId === id));
  }

  const { Option } = Select;

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

  const closeNewLocationDrawer = async (id: number | null) => {
    await loadLocations();
    form.setFieldValue('locationId', id);
    setOpenNewLocationDrawer(false);
  }

  return (
    <Form
      form={form}
      labelCol={{ span: 4 }}
      wrapperCol={{ span: 20 }}
      layout="horizontal"
      autoComplete='off'
      hideRequiredMark
      onFinish={triggerSubmit}
      initialValues={initialValues}
    >
      <Form.Item
        name="gameId"
        style={{ marginBottom: 10 }}
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
        label={t('common.location')}
        style={{ marginBottom: 10 }}
      >
        <Space.Compact style={{ width: '100%' }}>
          <Form.Item
            name="locationId"
            valuePropName="value"
            noStyle
          >
            <Select
              placeholder={t('play.new.location.placeholder')}
              loading={loadingLocations}
              showSearch
              filterOption={(input, option) => ((option?.children ?? '') as string).toLowerCase().includes(input.toLowerCase())}
            >
              {locations.map(x => <Option key={x.id} value={x.id} >{x.name}</Option>)}
            </Select>
          </Form.Item>
          <Button icon={<PlusOutlined />} type="primary" onClick={() => setOpenNewLocationDrawer(true)}>Add new</Button>
        </Space.Compact>
      </Form.Item>
      <Form.Item
        name="players"
        label={t('common.players')}
        style={{ marginBottom: 10 }}
      >
        <Space direction='vertical' style={{ width: '100%' }}>
          <Button
            type="primary"
            onClick={() => createNewPlayer()}
            icon={<PlusOutlined />}
          >
            {t('play.new.player.add')}
          </Button>
          <List
            locale={{ emptyText: t('play.new.player.empty') }}
            size="small"
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
                          <Avatar src={`/${player?.image ?? ""}`} />
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
      <Form.Item
        label={t('play.new.start.title')}
        style={{ marginBottom: 10 }}
      >
        <Space.Compact>
          <Form.Item
            name="start"
            noStyle
            rules={[{ required: true, message: t('play.new.start.datetime-required') }]}
          >
            <DatePicker
              style={{ width: 'calc(100% - 30px)' }}
              placeholder={t('play.new.start.start-placeholder')}
              format={`${convertToAntdFormat(settings.dateTimeFormat)}`}
              showTime
              minuteStep={5}
            />
          </Form.Item>
          <Form.Item
            name='minutes'
            noStyle
            rules={[{ required: true, message: t('play.new.start.length-required') }]}
          >
            <Input style={{ width: '70%' }} placeholder={t('play.new.start.length-placeholder')} />
          </Form.Item>
        </Space.Compact>
      </Form.Item>
      <Form.Item 
      label={t('play.new.ended.title')} 
      name="ended" 
      valuePropName="checked"
      style={{ marginBottom: 10 }}
      >
        <Checkbox>{t('play.new.ended.description')}</Checkbox>
      </Form.Item>
      <Form.Item 
      label={t('common.comment')} 
      name="comment"
      style={{ marginBottom: 10 }}
      >
        <Input.TextArea rows={4} />
      </Form.Item>
      <Form.Item wrapperCol={{ offset: 4, span: 20 }}>
        <Button
          type="primary"
          htmlType="submit"
          block
        >
          {t('play.new.save')}
        </Button>
      </Form.Item>
      <NewLocationDrawer open={openNewLocationDrawer} onClose={closeNewLocationDrawer} />
      <PlayerSelectorDrawer open={openPlayerSelector} close={closeNewPlayerDrawer} edit={playerToEdit} nextId={nextId} />
    </Form >
  )
}