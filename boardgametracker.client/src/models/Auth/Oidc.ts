import { z } from "zod";

export interface OidcProviderSummary {
	id: number;
	name: string;
	displayName: string;
	enabled: boolean;
}

export interface OidcProviderConfig extends OidcProviderSummary {
	authority: string;
	clientId: string;
	hasClientSecret: boolean;
	scopes: string;
	autoProvisionUsers: boolean;
	authorizationEndpoint: string | null;
	tokenEndpoint: string | null;
	userInfoEndpoint: string | null;
	usernameClaimType: string | null;
	emailClaimType: string | null;
	displayNameClaimType: string | null;
	rolesClaimType: string | null;
	adminGroupValue: string | null;
	iconUrl: string | null;
	buttonColor: string | null;
}

export interface OidcSetup {
	publicBaseUrl: string;
	publicUrlConfigured: boolean;
	callbackUriTemplate: string;
	linkCallbackUriTemplate: string;
}

export interface OidcDiscoveryResult {
	issuer: string;
	authorizationEndpoint: string;
	tokenEndpoint: string;
	userInfoEndpoint: string;
	issuerMatchesAuthority: boolean;
}

export interface ExternalLogin {
	id: number;
	provider: string;
	providerKey: string;
	providerDisplayName: string | null;
	linkedAt: Date;
	lastUsedAt: Date | null;
}

const optionalText = z
	.string()
	.trim()
	.transform((value) => (value === "" ? null : value));

export const OidcProviderSchema = z.object({
	name: z
		.string()
		.trim()
		.min(1, { message: "settings:sso.validation.name" })
		.max(100, { message: "settings:sso.validation.name" })
		.regex(/^[a-z0-9][a-z0-9-]*$/, { message: "settings:sso.validation.name" }),
	displayName: z.string().trim().min(1, { message: "settings:sso.validation.display-name" }).max(200),
	authority: z
		.string()
		.trim()
		.url({ message: "settings:sso.validation.authority" })
		.refine((value) => value.startsWith("https://") || value.startsWith("http://"), {
			message: "settings:sso.validation.authority",
		}),
	clientId: z.string().trim().min(1, { message: "settings:sso.validation.client-id" }),
	clientSecret: z.string(),
	scopes: z.string().trim().min(1, { message: "settings:sso.validation.scopes" }),
	enabled: z.boolean(),
	autoProvisionUsers: z.boolean(),
	usernameClaimType: optionalText,
	emailClaimType: optionalText,
	displayNameClaimType: optionalText,
	rolesClaimType: optionalText,
	adminGroupValue: optionalText,
});

export type OidcProviderForm = z.input<typeof OidcProviderSchema>;
export type OidcProviderRequest = z.output<typeof OidcProviderSchema>;
