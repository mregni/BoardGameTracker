import { Settings } from "lucide-react";
import { useTranslation } from "react-i18next";
import {
	Paragraph,
	SectionHeader,
	Table,
} from "../../../components/documentation";

const columns = [
	{
		headerKey: "getting-started:envVars.table.configOption",
		key: "config",
		translationKey: true,
	},
	{ headerKey: "getting-started:envVars.table.variableName", key: "variable" },
	{ headerKey: "getting-started:envVars.table.defaultValue", key: "default" },
	{
		headerKey: "getting-started:envVars.table.info",
		key: "info",
		translationKey: true,
	},
];

const rows = [
	{
		config: "getting-started:envVars.rows.timezone.config",
		variable: "TZ",
		default: "UTC",
	},
	{
		config: "getting-started:envVars.rows.jwtSecret.config",
		variable: "JWT_SECRET",
		info: "getting-started:envVars.rows.jwtSecret.info",
	},
	{
		config: "getting-started:envVars.rows.dbHost.config",
		variable: "DB_HOST",
		default: "db",
	},
	{
		config: "getting-started:envVars.rows.dbPort.config",
		variable: "DB_PORT",
		default: "5432",
	},
	{
		config: "getting-started:envVars.rows.dbName.config",
		variable: "DB_NAME",
		default: "boardgametracker",
	},
	{
		config: "getting-started:envVars.rows.dbUser.config",
		variable: "DB_USER",
		default: "dbuser",
	},
	{
		config: "getting-started:envVars.rows.dbPassword.config",
		variable: "DB_PASSWORD",
		info: "getting-started:envVars.rows.dbPassword.info",
	},
	{
		config: "getting-started:envVars.rows.logLevel.config",
		variable: "LOGLEVEL",
		default: "warn",
		info: "getting-started:envVars.rows.logLevel.info",
	},
	{
		config: "getting-started:envVars.rows.sentryLogging.config",
		variable: "STATISTICS_ENABLED",
		default: "false",
		info: "getting-started:envVars.rows.sentryLogging.info",
	},
	{
		config: "getting-started:envVars.rows.authBypass.config",
		variable: "AUTH_BYPASS",
		default: "false",
		info: "getting-started:envVars.rows.authBypass.info",
	},
];

export const EnvironmentVariables = () => {
	const { t } = useTranslation();

	return (
		<section id="environment-variables">
			<SectionHeader
				icon={Settings}
				title={t("getting-started:envVars.title")}
			/>
			<div className="space-y-4">
				<Paragraph translationKey="getting-started:envVars.description" />

				<Table
					columns={columns}
					rows={rows}
					headers={columns.map((col) => t(col.headerKey))}
				/>
			</div>
		</section>
	);
};
