import {Button, Col, Dropdown, MenuProps, Row, Space, Typography} from 'antd';
import {ReactNode} from 'react';
import {Link, useNavigate} from 'react-router-dom';

import {ArrowLeftOutlined, MoreOutlined} from '@ant-design/icons';

import {useScreenInfo} from '../../hooks/useScreenInfo';

const { Title } = Typography;

export interface GcMenuItem {
  buttonType: 'link' | 'text' | 'ghost' | 'default' | 'primary' | 'dashed' | undefined;
  icon: ReactNode;
  onClick?: () => void;
  to?: string;
  content: string;
}

interface HeaderProps {
  isLoading: boolean;
  title: string;
  hasBack?: boolean;
  backNavigation?: string;
  items?: GcMenuItem[];
}

export const GcPageHeader = (props: HeaderProps) => {
  const { isLoading, title, items = [], hasBack = false, backNavigation = '' } = props;
  const navigate = useNavigate();
  const { screenMap } = useScreenInfo();

  const dropdownItems: MenuProps['items'] =
    items.map((item, i) => { return { key: i, icon: item.icon, label: <Link to={item.to ?? ''} onClick={item.onClick}>{item.content}</Link> } })

  return (
    <Row justify="space-between" align='middle' style={{ width: '100%' }}>
      <Col>
        <Space wrap>
          {
            hasBack && (
              <Button
                type="text"
                size='large'
                icon={<ArrowLeftOutlined />}
                onClick={() => navigate(backNavigation)}
                disabled={isLoading}
              />
            )
          }
          <Title level={screenMap.lg ? 3 : 5} style={{ margin: 0, paddingLeft: (hasBack ? 0 : 10) }}>{title}</Title>
        </Space>
      </Col>
      {
        screenMap.lg !== undefined && (
          <Col>
            <Space wrap>
              {!screenMap.lg &&
                <Dropdown menu={{ items: dropdownItems }} placement="bottomRight" arrow={{ pointAtCenter: true }}>
                  <Button icon={<MoreOutlined />} size='large' type='ghost'></Button>
                </Dropdown>
              }
              {screenMap.lg &&
                (items.map((item) => <Button
                  key={item.content}
                  icon={item.icon}
                  type={item.buttonType}
                  onClick={item.onClick}
                  disabled={isLoading}
                >
                  {item.content}
                </Button>))
              }
            </Space>
          </Col>
        )
      }
    </Row>
  )
};