require 'sinatra/base'
require 'json'
require 'octokit'

class CI < Sinatra::Base

  # !!! DO NOT EVER USE HARD-CODED VALUES IN A REAL APP !!!
  # Instead, set and test environment variables, like below
  ACCESS_TOKEN = ENV['MY_PERSONAL_TOKEN']

  before do
    @client ||= Octokit::Client.new(:access_token => ACCESS_TOKEN)
  end

  post '/continuous-integration/elskom/ci/pr' do
    @payload = JSON.parse(params[:payload])

    case request.env['HTTP_X_GITHUB_EVENT']
    when "pull_request"
      if @payload["action"] == "opened"
        process_pull_request(@payload["pull_request"])
      end
      # handle reopened action too.
      if @payload["action"] == "reopened"
        process_pull_request(@payload["pull_request"])
      end
      if @payload["action"] == "closed"
        process_pull_request_closed(@payload["pull_request"])
      end
      if @payload["action"] == "labeled"
        process_pull_request(@payload["pull_request"])
      end
      if @payload["action"] == "unlabeled"
        process_pull_request(@payload["pull_request"])
      end
    end
  end

  helpers do
    def process_pull_request_closed(pull_request)
      # todo: possibly check if pull requesed branch
      # is in the same repository as
      # pull_request['base']['repo']
      # and if it is comment on closed pull request
      # if the repo owner, admins, pull requestee
      # wants it deleted or not if the CI has
      # permision to.
    end

    def process_pull_request(pull_request)
      # todo: read comments from repo /org admins though for things like
      # "I approve this" and then approve the changes and then wait for
      # "merge squashed/rebased/commit" (if enabled on repository).
      @client.create_status(pull_request['base']['repo']['full_name'], pull_request['head']['sha'], 'pending')
      @client.labels_for_issue(pull_request['base']['repo']['full_name'], pull_request['number']).each do |label|
        # I am not sure if this is correct or not.
        next unless label[:name] == "skip news"
        @client.create_status(pull_request['base']['repo']['full_name'], pull_request['head']['sha'], 'success')
        puts "'skip news' label found!"
        return unless label[:name] != "skip news"
      end
      @client.pull_request_files(pull_request['base']['repo']['full_name'], pull_request['number']).each do |file|
        # I am not sure if this is correct or not.
        next unless file[:filename].start_with?("Misc/NEWS")
        @client.create_status(pull_request['base']['repo']['full_name'], pull_request['head']['sha'], 'success')
        puts "Misc/NEWS entry found!"
        return unless !file[:filename].starts_with?("Misc/NEWS")
      end
      @client.create_status(pull_request['base']['repo']['full_name'], pull_request['head']['sha'], 'failure')
      puts "Misc/NEWS entry not found and 'skip news' is not added!"
    end
  end
end
